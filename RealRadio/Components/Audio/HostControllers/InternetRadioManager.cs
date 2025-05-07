using System;
using AudioStreamer.MediaFoundation;
using NAudio.Wave;
using UnityEngine;

namespace RealRadio.Components.Audio.HostControllers;

public class InternetRadioController : HostController
{
    protected override void Awake()
    {
        base.Awake();

        Host.AudioStream = new MediaFoundationAudioStream(Station.Url ?? throw new ArgumentException("Internet radio station has no URL set"), resetReaderAtEof: false)
        {
            ResampleFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: AudioSettings.GetSampleRate(), channels: 2),
        };
    }
}
