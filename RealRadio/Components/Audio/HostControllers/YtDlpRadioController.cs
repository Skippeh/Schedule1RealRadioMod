using System;
using System.Collections;
using System.Collections.Generic;
using RealRadio.Components.YoutubeDL;
using UnityEngine;

namespace RealRadio.Components.Audio.HostControllers;

public class YtDlpRadioController : HostController
{
    protected override void Awake()
    {
        base.Awake();

        if (Station.Type != RadioType.YtDlp)
            throw new InvalidOperationException($"Invalid radio type: {Station.Type} (expected {RadioType.InternetRadio})");
    }
}
