using System;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI;
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
    private Label noActiveDownloadsLabel = null!;

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

    public bool DownloadIndicatorEnabled
    {
        get => downloadIndicator.classList.Contains("enabled");
        set
        {
            if (value && !DownloadIndicatorEnabled)
                downloadIndicator.AddToClassList("enabled");
            else if (!value && DownloadIndicatorEnabled)
                downloadIndicator.RemoveFromClassList("enabled");
        }
    }

    public bool NoActiveDownloadsLabelVisible
    {
        get => noActiveDownloadsLabel.classList.Contains("visible");
        set
        {
            if (value && !NoActiveDownloadsLabelVisible)
                noActiveDownloadsLabel.AddToClassList("visible");
            else if (!value && NoActiveDownloadsLabelVisible)
                noActiveDownloadsLabel.RemoveFromClassList("visible");
        }
    }

    public override void Awake()
    {
        base.Awake();

        Document = GetComponent<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object");
        downloadIndicator = Document.rootVisualElement.Query(name: "DownloadIndicator").First() ?? throw new InvalidOperationException("Could not find download indicator ui element");
        downloadIndicatorSpinIcon = downloadIndicator.Query(className: "icon-spinning").First() ?? throw new InvalidOperationException("Could not find download indicator's spin icon ui element");
        noActiveDownloadsLabel = Document.rootVisualElement.Query<Label>(name: "NoActiveDownloadsLabel").First() ?? throw new InvalidOperationException("Could not find no active downloads label ui element");

        foreach (var (url, progress) in YtDlpManager.Instance.DownloadProgresses)
            OnDownloadProgressUpdate(url, progress);

        YtDlpManager.Instance.OnDownloadProgress += OnDownloadProgressUpdate;
    }

    private void OnDownloadProgressUpdate(string url, DownloadProgress progress)
    {
    }

    private void Update()
    {
        bool inGameAndNotPaused = PauseMenu.InstanceExists && !PauseMenu.Instance.IsPaused;
        bool downloadsInProgress = YtDlpManager.Instance.DownloadProgresses.Count > 0;
        DownloadIndicatorVisible = downloadsInProgress;
        DownloadIndicatorEnabled = inGameAndNotPaused;
        NoActiveDownloadsLabelVisible = !downloadsInProgress;

        if (inGameAndNotPaused)
        {
            downloadIndicatorSpinIcon.style.rotate = new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle((float)(Time.realtimeSinceStartupAsDouble * LoadingIconSpinDegreesPerSecond % 360d), AngleUnit.Degree)));
        }
    }
}
