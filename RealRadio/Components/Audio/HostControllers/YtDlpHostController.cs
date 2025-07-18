using System;
using System.Collections;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;
using RealRadio.Components.Radio;
using RealRadio.Components.YoutubeDL;
using RealRadio.Data;
using RealRadio.Events;
using UnityEngine;

namespace RealRadio.Components.Audio.HostControllers;

public class YtDlpHostController : HostController
{
    private uint? currentSongIteration;
    private ushort? lastSongIndex;
    private Coroutine? downloadAndPlayAudioFileCoroutine;

    private bool PlayState(RadioStationState state)
    {
        if (!state.IsValid())
        {
            Logger.LogWarning($"Can't play invalid song state: {state}");
            return false;
        }

        if (state.SongIndex >= Station.Urls!.Length)
        {
            Logger.LogWarning($"Received out of bounds song index: {state.SongIndex} (max {Station.Urls.Length - 1})");
            return false;
        }

        Logger.LogDebug($"Play state: {state}");

        currentSongIteration = state.SongIteration.Value;
        lastSongIndex = state.SongIndex.Value;

        if (downloadAndPlayAudioFileCoroutine != null)
            StopCoroutine(downloadAndPlayAudioFileCoroutine);

        downloadAndPlayAudioFileCoroutine = StartCoroutine(DownloadAndPlayAudioFile(Station.Urls[state.SongIndex.Value], () => state.CurrentTime.Value));

        return true;
    }

    protected override void Awake()
    {
        base.Awake();

        if (Station.Type != RadioType.YtDlp)
            throw new InvalidOperationException($"Invalid radio type: {Station.Type} (expected {RadioType.YtDlp})");

        if (Station.Urls is null or { Length: 0 })
            throw new ArgumentException("YtDlp radio station has no URLs");

        RadioSyncManager.Instance.OnStateReceived += OnStateReceived;
        Host.OnStreamStartRequested += OnHostStreamStartRequested;
        Host.OnStreamStopped += OnHostStreamStopped;
        Host.OnStreamEnded += OnHostStreamEnded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (RadioSyncManager.Instance)
            RadioSyncManager.Instance.OnStateReceived -= OnStateReceived;
    }

    private void OnHostStreamEnded()
    {
        RadioSyncManager.Instance.RequestOrSetSongState(Station, RadioSyncManager.GetRandomRadioStationState(Station, lastSongIndex, currentSongIteration + 1, startTime: 0));
    }

    private void OnHostStreamStartRequested(EventRefData<bool> preventStart)
    {
        var state = RadioSyncManager.Instance.GetLocalState(Station);

        if (Host.AudioStream == null)
            preventStart.Value = true;

        if (state == null || !state.IsValid())
        {
            preventStart.Value = true;
            RadioSyncManager.Instance.RequestOrSetSongState(Station, RadioSyncManager.GetRandomRadioStationState(Station, lastSongIndex, currentSongIteration));
        }
        else if (state.SongIteration != currentSongIteration)
        {
            PlayState(state);
        }
    }

    private void OnHostStreamStopped()
    {
        currentSongIteration = null;
    }

    private void OnStateReceived(RadioStation station, RadioStationState state)
    {
        if (station.Id != Station.Id)
            return;

        // This event is potentially invoked before OnRadioStationUpdated
        // so we need to update the radio station here
        Station = station;

        if (state.SongIteration == currentSongIteration)
            return;

        PlayState(state);
    }

    private IEnumerator DownloadAndPlayAudioFile(string url, Func<float>? getStartTime = null)
    {
        var task = YtDlpManager.Instance.DownloadAudioFile(url);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Logger.LogError($"Failed to download audio file '{url}':\n{task.Exception}");
            downloadAndPlayAudioFileCoroutine = null;
            yield break;
        }

        var filePathUrl = GetFileUrl(task.Result);

        // we need to save and restore the current iteration because it's set to null when the stream is stopped
        var songIteration = currentSongIteration;

        Host.StopAudioStream();
        yield return new WaitUntil(() => Host.AudioStream == null || !Host.AudioStream.Started);

        Logger.LogDebug($"Playing audio file '{filePathUrl}'");

        Host.AudioStream = new MediaFoundationAudioStream(filePathUrl)
        {
            ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: AudioSettings.GetSampleRate(), channels: 2),
        };

        // restore song iteration
        currentSongIteration = songIteration;

        Host.StartAudioStream();

        float startTime = getStartTime?.Invoke() ?? 0f;
        if (startTime > 0)
        {
            yield return new WaitUntil(() => Host.AudioStream != null && Host.AudioStream.Started);

            if (Host.AudioStream.CanSeek)
                Host.AudioStream.CurrentTime = TimeSpan.FromSeconds(startTime);
            else
                Logger.LogWarning($"Audio stream '{filePathUrl}' does not support seeking");
        }

        downloadAndPlayAudioFileCoroutine = null;
    }

    private string GetFileUrl(string filePath)
    {
        return $"file:///{filePath}";
    }
}
