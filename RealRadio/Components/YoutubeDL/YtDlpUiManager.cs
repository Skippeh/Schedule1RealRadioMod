using System;
using System.Collections.Generic;
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

    [field: SerializeField]
    public VisualTreeAsset ListItemAsset { get; private set; } = null!;

    private VisualElement downloadIndicator = null!;
    private VisualElement downloadIndicatorSpinIcon = null!;
    private Label noActiveDownloadsLabel = null!;
    private ScrollView itemsContainer = null!;

    private Dictionary<string, ListItem> items = [];

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
        itemsContainer = Document.rootVisualElement.Query<ScrollView>(name: "ListItemsContainer").First() ?? throw new InvalidOperationException("Could not find item list container ui element");

        if (ListItemAsset == null)
            throw new InvalidOperationException("ListItemAsset is not set");

        foreach (var (url, progress) in YtDlpManager.Instance.DownloadProgresses)
            OnDownloadProgressUpdate(url, progress);

        YtDlpManager.Instance.OnDownloadProgress += OnDownloadProgressUpdate;
    }

    private void OnDownloadProgressUpdate(string url, DownloadProgress progress)
    {
        if (!items.TryGetValue(url, out var item))
        {
            VisualElement element = ListItemAsset.Instantiate();
            itemsContainer.Add(element);
            item = new ListItem(element);
            items.Add(url, item);

            var metaData = YtDlpManager.Instance.AudioMetaData[url];
            item.Name = $"{metaData.Title} ({metaData.Uploader})";
        }

        item.StateText = progress.State.ToString();

        switch (progress.State)
        {
            case DownloadState.PreProcessing:
                item.Progress = 0f;
                break;
            case DownloadState.Downloading:
                if (progress.Progress > 0f)
                    item.Progress = progress.Progress * 0.9f;

                break;
            case DownloadState.Success:
                item.Progress = 1f;
                break;
            case DownloadState.Error:
                item.Progress = 1f;
                break;
            default:
                item.Progress = 0.9f;
                break;
        }

        if (progress.State == DownloadState.Success)
        {
            items.Remove(url);
            itemsContainer.Remove(item.Element);
        }
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

    class ListItem
    {
        public string Name
        {
            get => name.text;
            set => name.text = value;
        }

        public float Progress
        {
            get => progressBar.value;
            set => progressBar.value = value;
        }

        public string StateText
        {
            get => state.text;
            set => state.text = value;
        }

        public VisualElement Element { get; private set; }

        private Label name;
        private Label state;
        private ProgressBar progressBar;

        public ListItem(VisualElement element)
        {
            Element = element;

            name = element.Query<Label>(name: "Name").First() ?? throw new InvalidOperationException("Could not find name ui element");
            state = element.Query<Label>(name: "State").First() ?? throw new InvalidOperationException("Could not find state ui element");
            progressBar = element.Query<ProgressBar>(name: "ProgressBar").First() ?? throw new InvalidOperationException("Could not find progress bar ui element");
            progressBar.value = 0f;
        }
    }
}
