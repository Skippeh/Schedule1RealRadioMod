using System;
using RealRadio.Data;
using UnityEngine;

namespace RealRadio.Components.Audio.HostControllers;

[RequireComponent(typeof(StreamAudioHost))]
public class HostController : MonoBehaviour
{
    [NonSerialized]
    public RadioStation Station = null!;

    public StreamAudioHost Host { get; private set; } = null!;

    protected virtual void Awake()
    {
        if (Station == null)
            throw new InvalidOperationException(nameof(Station));

        Host = GetComponent<StreamAudioHost>() ?? throw new InvalidOperationException("No audio host component found on game object");
    }
}
