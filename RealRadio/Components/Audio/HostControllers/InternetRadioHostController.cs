using System;
using System.Collections;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;
using RealRadio.Data;
using UnityEngine;

namespace RealRadio.Components.Audio.HostControllers;

public class InternetRadioHostController : HostController
{
    protected override void Awake()
    {
        base.Awake();

        if (Station.Type != RadioType.InternetRadio)
            throw new InvalidOperationException($"Invalid radio type: {Station.Type} (expected {RadioType.InternetRadio})");
    }

    protected override void OnStationChanged(RadioStation newStation, RadioStation? oldStation)
    {
        if (newStation.Url == oldStation?.Url)
            return;

        if (Host.AudioStream != null)
        {
            StartCoroutine(RestartStream());
            return;
        }

        SetAudioStream();
    }

    private IEnumerator RestartStream()
    {
        Host.StopAudioStream();

        yield return new WaitUntil(() => Host.AudioStream == null || !Host.AudioStream.Started);
        SetAudioStream();
        Host.StartAudioStream();
    }

    private void SetAudioStream()
    {
        Host.AudioStream = new MediaFoundationAudioStream(Station.Url ?? throw new ArgumentException("Internet radio station has no URL set"), resetReaderAtEof: false)
        {
            ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: AudioSettings.GetSampleRate(), channels: 2),
        };
    }
}
