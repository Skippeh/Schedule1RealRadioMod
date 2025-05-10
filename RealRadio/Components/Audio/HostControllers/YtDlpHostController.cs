using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;
using RealRadio.Components.YoutubeDL;
using RealRadio.Data;
using ScheduleOne;
using UnityEngine;

namespace RealRadio.Components.Audio.HostControllers;

public class YtDlpHostController : HostController
{
    private uint currentSongIteration;

    public void Play(string url, float startTime)
    {
        StartCoroutine(DownloadAndPlayAudioFile(url, startTime));
    }

    protected override void Awake()
    {
        base.Awake();

        if (Station.Type != RadioType.YtDlp)
            throw new InvalidOperationException($"Invalid radio type: {Station.Type} (expected {RadioType.InternetRadio})");

        if (Station.Urls is null or { Length: 0 })
            throw new ArgumentException("YtDlp radio station has no URLs");

        RadioSyncManager.Instance.OnStateReceived += OnStateReceived;
        RadioSyncManager.Instance.RequestOrSetSongState(Station, GetRandomRadioStationState(currentSongIteration + 1));
    }

    private void OnStateReceived(RadioStation station, RadioStationState state)
    {
        if (station != Station)
            return;

        if (state.SongIteration == currentSongIteration)
            return;

        Plugin.Logger.LogInfo($"Received song state: {state}");

        currentSongIteration = state.SongIteration!.Value;
        Play(Station.Urls![state.SongIndex!.Value], state.CurrentTime!.Value);
    }

    private IEnumerator DownloadAndPlayAudioFile(string url, float startTime)
    {
        var task = YtDlpManager.Instance.DownloadAudioFile(url);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Plugin.Logger.LogError($"Failed to download audio file '{url}':\n{task.Exception}");
            yield break;
        }

        var filePathUrl = GetFileUrl(task.Result);

        Host.StopAudioStream();
        yield return new WaitUntil(() => Host.AudioStream == null || !Host.AudioStream.Started);

        Plugin.Logger.LogInfo($"Playing audio file '{filePathUrl}'");

        Host.AudioStream = new MediaFoundationAudioStream(filePathUrl, resetReaderAtEof: false)
        {
            ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: AudioSettings.GetSampleRate(), channels: 2),
        };

        Host.StartAudioStream();

        if (startTime > 0)
        {
            yield return new WaitUntil(() => Host.AudioStream != null && Host.AudioStream.Started);

            if (Host.AudioStream.CanSeek)
                Host.AudioStream.SeekToTime(TimeSpan.FromSeconds(startTime));
            else
                Plugin.Logger.LogWarning($"Audio stream '{filePathUrl}' does not support seeking");
        }
    }

    private string GetFileUrl(string filePath)
    {
        return $"file:///{filePath}";
    }

    private RadioStationState GetRandomRadioStationState(uint iteration)
    {
        var result = new RadioStationState();
        ushort index = (ushort)UnityEngine.Random.Range(0, Station.Urls!.Length);

        result.SongIndex = index;
        result.SongIteration = iteration;
        result.CurrentTime = 0; // todo: get random value limited by song duration in seconds

        return result;
    }
}
