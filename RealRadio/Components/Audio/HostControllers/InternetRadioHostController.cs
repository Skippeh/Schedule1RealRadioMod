using System;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;
using UnityEngine;

namespace RealRadio.Components.Audio.HostControllers;

public class InternetRadioHostController : HostController
{
    protected override void Awake()
    {
        base.Awake();

        if (Station.Type != RadioType.InternetRadio)
            throw new InvalidOperationException($"Invalid radio type: {Station.Type} (expected {RadioType.InternetRadio})");

        Host.AudioStream = new MediaFoundationAudioStream(Station.Url ?? throw new ArgumentException("Internet radio station has no URL set"), resetReaderAtEof: false)
        {
            ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: AudioSettings.GetSampleRate(), channels: 2),
        };
    }
}
