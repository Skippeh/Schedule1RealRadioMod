using System;
using System.Collections;
using System.Collections.Generic;
using RealRadio.Components.YoutubeDL;
using UnityEngine;

namespace RealRadio.Components.Audio.HostControllers;

public class YtDlpRadioController : HostController
{
    private Dictionary<int, string> audioFilePaths = [];

    protected override void Awake()
    {
        base.Awake();

        if (Station.Type != RadioType.YtDlp)
            throw new InvalidOperationException($"Invalid radio type: {Station.Type} (expected {RadioType.InternetRadio})");

        if (Station.Urls is null or { Length: 0 })
            throw new ArgumentException("YtDlp radio station has no URLs");

        for (int i = 0; i < Station.Urls.Length; ++i)
        {
            StartCoroutine(DownloadAudioFileCoroutine(Station.Urls[i], i));
        }
    }

    private IEnumerator DownloadAudioFileCoroutine(string url, int dictionaryKey)
    {
        var task = YtDlpManager.Instance.DownloadAudioFile(url);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Plugin.Logger.LogError($"Failed to download audio file '{url}':\n{task.Exception}");
            yield break;
        }

        audioFilePaths[dictionaryKey] = task.Result;
    }
}
