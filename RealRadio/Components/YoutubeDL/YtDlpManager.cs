using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RealRadio.Components.Radio;
using RealRadio.Data;
using ScheduleOne.DevUtilities;
using UnityEngine;
using YoutubeDLSharp.Metadata;

// todo: rename to something else since it supports more than youtube (but not YtDlp because that conflicts with the YtDlp class)
namespace RealRadio.Components.YoutubeDL;

public class YtDlpManager : PersistentSingleton<YtDlpManager>
{
    public readonly IReadOnlyDictionary<string, VideoData> AudioMetaData;
    public readonly IReadOnlyDictionary<string, string> AudioFilePaths;

    private YtDlp ytDlp = null!;
    private Task downloadBinariesTask = null!;
    private readonly CancellationTokenSource ytDlpCts = new();

    private readonly Dictionary<string, VideoData> metaDataPaths = new();
    private readonly Dictionary<string, string> audioFilePaths = new();

    public YtDlpManager()
    {
        AudioMetaData = new ReadOnlyDictionary<string, VideoData>(metaDataPaths);
        AudioFilePaths = new ReadOnlyDictionary<string, string>(audioFilePaths);
    }

    public override void Awake()
    {
        base.Awake();

        string cachePath = GetCachePath();
        ytDlp = new YtDlp(cachePath);
        StartCoroutine(DownloadBinaries());

        RadioStationManager.Instance.StationAdded += OnRadioStationAdded;

        foreach (var station in RadioStationManager.Instance.Stations)
            OnRadioStationAdded(station);
    }

    private static string GetCachePath()
    {
        var assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(assemblyDir, "Cache");
    }

    private void OnRadioStationAdded(RadioStation station)
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
        if (metaDataPaths.TryGetValue(url, out var metaData))
            return metaData;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? _))
            throw new UriFormatException($"Invalid URL '{url}'");

        metaData = await ytDlp.DownloadMetaData(url, ytDlpCts.Token);
        metaDataPaths[url] = metaData;
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
        var audioTask = ytDlp.DownloadAudioFile(url, ytDlpCts.Token);

        var metaData = await metaDataTask;

        if (metaData.IsLive == true)
        {
            throw new ArgumentException("Live streams are not supported");
        }

        var filePath = await audioTask;
        audioFilePaths[url] = filePath;
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
}
