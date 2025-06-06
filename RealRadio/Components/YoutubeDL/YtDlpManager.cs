using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Newtonsoft.Json;
using RealRadio.Components.Radio;
using RealRadio.Data;
using ScheduleOne.DevUtilities;
using UnityEngine;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

// todo: rename to something else since it supports more than youtube (but not YtDlp because that conflicts with the YtDlp class)
namespace RealRadio.Components.YoutubeDL;

public class YtDlpManager : PersistentSingleton<YtDlpManager>
{
    public readonly IReadOnlyDictionary<string, VideoData> AudioMetaData;
    public readonly IReadOnlyDictionary<string, string> AudioFilePaths;
    public readonly IReadOnlyDictionary<string, DownloadProgress> DownloadProgresses;

    public Action<string, DownloadProgress>? OnDownloadProgress;

    private YtDlp ytDlp = null!;
    private Task downloadBinariesTask = null!;
    private readonly CancellationTokenSource ytDlpCts = new();

    private readonly Dictionary<string, VideoData> metaData = new();
    private readonly Dictionary<string, string> audioFilePaths = new();
    private readonly Dictionary<string, DownloadProgress> downloadProgresses = new();
    private readonly Dictionary<string, DownloadProgress> downloadProgressUpdates = new(); // mutated by ReportDownloadProgress from a different thread
    private readonly object downloadProgressUpdatesLock = new();
    private readonly HashSet<string> finishedDownloads = new();

    public YtDlpManager()
    {
        AudioMetaData = new ReadOnlyDictionary<string, VideoData>(metaData);
        AudioFilePaths = new ReadOnlyDictionary<string, string>(audioFilePaths);
        DownloadProgresses = new ReadOnlyDictionary<string, DownloadProgress>(downloadProgresses);
    }

    public override void Awake()
    {
        base.Awake();

        string cachePath = GetCachePath();
        ytDlp = new YtDlp(cachePath);
        StartCoroutine(DownloadBinaries());

        RadioStationManager.Instance.StationUpdated += OnRadioStationUpdated;

        foreach (var station in RadioStationManager.Instance.Stations)
            OnRadioStationUpdated(station, oldStation: null);
    }

    private void LateUpdate()
    {
        finishedDownloads.Clear();

        // remove successful downloads
        foreach (var (url, progress) in downloadProgresses)
        {
            if (progress.State == DownloadState.Success)
                finishedDownloads.Add(url);
        }

        foreach (var url in finishedDownloads)
        {
            downloadProgresses.Remove(url);
        }

        // add updates from other threads
        lock (downloadProgressUpdatesLock)
        {
            foreach (var (url, progress) in downloadProgressUpdates)
            {
                downloadProgresses[url] = progress;
                OnDownloadProgress?.Invoke(url, progress);
            }

            downloadProgressUpdates.Clear();
        }
    }

    private static string GetCachePath()
    {
        var assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(assemblyDir, "Cache");
    }

    private void OnRadioStationUpdated(RadioStation station, RadioStation? oldStation)
    {
        if (station.Type != RadioType.YtDlp)
            return;

        if (station.Urls is null or { Length: 0 })
        {
            Plugin.Logger.LogWarning($"YtDlp radio station '{station.Name}' has no URLs");
            return;
        }

        foreach (string url in station.Urls)
        {
            StartCoroutine(DownloadAudioFileCoroutine(url));
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        ytDlpCts.Cancel();
    }

    private IEnumerator DownloadBinaries()
    {
        Plugin.Logger.LogInfo("Downloading YtDlp binaries...");

        downloadBinariesTask = ytDlp.DownloadBinaries();
        yield return new WaitUntil(() => downloadBinariesTask.IsCompleted);

        if (downloadBinariesTask.IsFaulted)
        {
            Plugin.Logger.LogError($"Failed to download YtDlp binaries:\n{downloadBinariesTask.Exception}");
            yield break;
        }

        Plugin.Logger.LogInfo("YtDlp binaries downloaded");
    }

    public async Task<VideoData> FetchMetaData(string url)
    {
        if (this.metaData.TryGetValue(url, out var metaData))
            return metaData;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? _))
            throw new UriFormatException($"Invalid URL '{url}'");

        metaData = await ytDlp.DownloadMetaData(url, ytDlpCts.Token);
        this.metaData[url] = metaData;
        return metaData;
    }

    public async Task<string> DownloadAudioFile(string url)
    {
        if (audioFilePaths.TryGetValue(url, out var audioFilePath))
            return audioFilePath;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? _))
            throw new UriFormatException($"Invalid URL '{url}'");

        Plugin.Logger.LogInfo($"Downloading (if not cached) audio file '{url}'...");

        var metaDataTask = FetchMetaData(url);

        var metaData = await metaDataTask;

        if (metaData.IsLive == true)
        {
            throw new ArgumentException("Live streams are not supported");
        }

        string filePath = await ytDlp.DownloadAudioFile(url, ytDlpCts.Token, progress: new ReportDownloadProgress(this, url));
        audioFilePaths[url] = filePath;

        if (metaData.Duration == null)
        {
            Plugin.Logger.LogInfo($"Attempting to get duration from audio file '{filePath}'...");

            try
            {
                metaData.Duration = await GetAudioFileDuration(filePath);
                Plugin.Logger.LogInfo($"Duration of audio file '{filePath}' is {metaData.Duration} seconds");

                try
                {
                    await ytDlp.UpdateCachedMetaData(url, metaData);
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Failed to update cached metadata for audio file '{filePath}': {ex}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Failed to get duration from audio file '{filePath}': {ex}");
            }
        }

        return filePath;
    }

    private IEnumerator DownloadAudioFileCoroutine(string url)
    {
        var task = DownloadAudioFile(url);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Plugin.Logger.LogError($"Failed to download audio file '{url}':\n{task.Exception}");
            yield break;
        }

        Plugin.Logger.LogInfo($"Audio file '{url}' downloaded: {task.Result}");
    }

    /// <summary>
    /// Get the duration of an audio file in seconds.
    /// </summary>
    /// <param name="filePath">The path to the audio file.</param>
    public async Task<float> GetAudioFileDuration(string filePath)
    {
        return await Task.Run(() =>
        {
            var reader = new AudioFileReader(filePath);
            return (float)reader.TotalTime.TotalSeconds;
        });
    }

    private class ReportDownloadProgress(YtDlpManager manager, string url) : IProgress<DownloadProgress>
    {
        public void Report(DownloadProgress value)
        {
            lock (manager.downloadProgressUpdatesLock)
                manager.downloadProgressUpdates[url] = value;
        }
    }
}
