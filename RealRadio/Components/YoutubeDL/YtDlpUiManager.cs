using System;
using ScheduleOne.DevUtilities;
using UnityEngine;
using UnityEngine.UIElements;
using YoutubeDLSharp;

namespace RealRadio.Components.YoutubeDL;

[RequireComponent(typeof(UIDocument))]
public class YtDlpUiManager : PersistentSingleton<YtDlpUiManager>
{
    public float LoadingIconSpinDegreesPerSecond = 360f;

    public UIDocument Document { get; private set; } = null!;

    private VisualElement downloadIndicator = null!;
    private VisualElement downloadIndicatorSpinIcon = null!;

    public bool DownloadIndicatorVisible
    {
        get => downloadIndicator.classList.Contains("visible");
        set
        {
            if (value && !DownloadIndicatorVisible)
                downloadIndicator.AddToClassList("visible");
            else if (!value && DownloadIndicatorVisible)
                downloadIndicator.RemoveFromClassList("visible");
        }
    }

    public override void Awake()
    {
        base.Awake();

        Document = GetComponent<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object");
        downloadIndicator = Document.rootVisualElement.Query(name: "DownloadIndicator").First() ?? throw new InvalidOperationException("Could not find download indicator ui element");
        downloadIndicatorSpinIcon = downloadIndicator.Query(className: "icon-spinning").First() ?? throw new InvalidOperationException("Could not find download indicator's spin icon ui element");

        foreach (var (url, progress) in YtDlpManager.Instance.DownloadProgresses)
            OnDownloadProgressUpdate(url, progress);

        YtDlpManager.Instance.OnDownloadProgress += OnDownloadProgressUpdate;
    }

    private void OnDownloadProgressUpdate(string url, DownloadProgress progress)
    {
    }

    private void Update()
    {
        bool visible = YtDlpManager.Instance.DownloadProgresses.Count > 0;
        DownloadIndicatorVisible = visible;
        downloadIndicatorSpinIcon.style.rotate = new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle((float)(Time.realtimeSinceStartupAsDouble * LoadingIconSpinDegreesPerSecond % 360d), AngleUnit.Degree)));
    }
}
