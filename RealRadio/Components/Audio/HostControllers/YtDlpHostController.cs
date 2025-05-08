using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;
using RealRadio.Components.YoutubeDL;
using ScheduleOne;
using UnityEngine;

namespace RealRadio.Components.Audio.HostControllers;

public class YtDlpHostController : HostController
{
    public void Play(string url)
    {
        StartCoroutine(DownloadAndPlayAudioFile(url));
    }

    protected override void Awake()
    {
        base.Awake();

        if (Station.Type != RadioType.YtDlp)
            throw new InvalidOperationException($"Invalid radio type: {Station.Type} (expected {RadioType.InternetRadio})");

        if (Station.Urls is null or { Length: 0 })
            throw new ArgumentException("YtDlp radio station has no URLs");

        Play(Station.Urls[0]);
    }

    private IEnumerator DownloadAndPlayAudioFile(string url)
    {
        var task = YtDlpManager.Instance.DownloadAudioFile(url);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Plugin.Logger.LogError($"Failed to download audio file '{url}':\n{task.Exception}");
            yield break;
        }

        var filePathUrl = GetFileUrl(url);

        if (filePathUrl == null)
        {
            Plugin.Logger.LogError($"Could not find local filepath for audio file '{url}'");
            yield break;
        }

        Host.StopAudioStream();
        yield return new WaitUntil(() => Host.AudioStream == null || !Host.AudioStream.Started);

        Plugin.Logger.LogInfo($"Playing audio file '{url}'");

        Host.AudioStream = new MediaFoundationAudioStream(filePathUrl, resetReaderAtEof: false)
        {
            ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: AudioSettings.GetSampleRate(), channels: 2),
        };
        Host.StartAudioStream();
    }

    private string? GetFileUrl(string url)
    {
        if (!YtDlpManager.Instance.AudioFilePaths.TryGetValue(url, out var filePath))
            return null;

        return $"file:///{filePath}";
    }
}
