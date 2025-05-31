using System;
using RealRadio.Data;
using UnityEngine;

namespace RealRadio.Components.Audio.HostControllers;

[RequireComponent(typeof(StreamAudioHost))]
public class HostController : MonoBehaviour
{
    public RadioStation Station
    {
        get => station;
        set
        {
            if (value == station)
                return;

            var oldStation = station;
            station = value;

            // host is null if the object is in the process of being created
            if (Host != null)
                OnStationChanged(station, oldStation);
        }
    }

    public StreamAudioHost Host { get; private set; } = null!;

    private RadioStation station = null!;

    protected virtual void Awake()
    {
        if (Station == null)
            throw new InvalidOperationException(nameof(Station));

        Host = GetComponent<StreamAudioHost>() ?? throw new InvalidOperationException("No audio host component found on game object");
        OnStationChanged(Station, oldStation: null);
    }

    protected virtual void OnStationChanged(RadioStation newStation, RadioStation? oldStation)
    {
    }
}
