using System;
using System.Collections.Generic;
using RealRadio.Components.UI;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI;
using UnityEngine;
using UnityEngine.UIElements;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

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
    private VisualElement downloadList = null!;
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

    public bool DownloadListEnabled
    {
        get => downloadList.classList.Contains("enabled");
        set
        {
            if (value && !DownloadListEnabled)
            {
                downloadList.AddToClassList("enabled");
                SortItemsContainer();
            }
            else if (!value && DownloadListEnabled)
                downloadList.RemoveFromClassList("enabled");
        }
    }

    public override void Awake()
    {
        base.Awake();

        Document = GetComponent<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object");
        downloadIndicator = Document.rootVisualElement.Query(name: "DownloadIndicator").First() ?? throw new InvalidOperationException("Could not find download indicator ui element");
        downloadIndicatorSpinIcon = downloadIndicator.Query(className: "icon-spinning").First() ?? throw new InvalidOperationException("Could not find download indicator's spin icon ui element");
        downloadList = Document.rootVisualElement.Query(name: "DownloadList").First() ?? throw new InvalidOperationException("Could not find download list ui element");
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
            if (progress.State == DownloadState.Success)
                return;

            VisualElement element = ListItemAsset.Instantiate();
            itemsContainer.Add(element);
            item = new ListItem(this, element, url);
            element.userData = item;
            items.Add(url, item);
        }

        item.DownloadProgress = progress;

        VideoData? metaData = YtDlpManager.Instance.AudioMetaData.GetValueOrDefault(url);

        if (metaData != null)
            item.Name = $"{metaData.Title ?? url} ({metaData.Uploader ?? "Unknown uploader"})";
        else
            item.Name = url;

        item.StateText = progress.State.ToString();
        item.ProgressBarErrorState = progress.State == DownloadState.Error;
        item.LogButtonEnabled = progress.State == DownloadState.Error;
        item.DownloadButtonEnabled = progress.State == DownloadState.Error;
        item.RemoveButtonEnabled = progress.State == DownloadState.Error;

        if (progress.State == DownloadState.Error && !string.IsNullOrEmpty(progress.Data))
        {
            item.LogText += $"{progress.Data}\n";
        }

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
                item.StateText = "Failed - check the log for more info";
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

        if (DownloadListEnabled)
            SortItemsContainer();
    }

    private void SortItemsContainer()
    {
        itemsContainer.Sort((a, b) =>
        {
            var aItem = a.userData as ListItem;
            var bItem = b.userData as ListItem;

            if (aItem == null || bItem == null)
                return 0;

            if (aItem.DownloadProgress == null || bItem.DownloadProgress == null)
                return 0; // Should be unreachable

            var progressCompare = bItem.DownloadProgress.Progress.CompareTo(aItem.DownloadProgress.Progress);

            if (progressCompare != 0)
                return progressCompare;

            return aItem.Name.CompareTo(bItem.Name);
        });
    }

    private void Update()
    {
        bool inGameAndNotPaused = PauseMenu.InstanceExists && !PauseMenu.Instance.IsPaused;
        bool downloadsInProgress = YtDlpManager.Instance.DownloadProgresses.Count > 0;
        DownloadIndicatorVisible = downloadsInProgress;
        DownloadIndicatorEnabled = inGameAndNotPaused;
        NoActiveDownloadsLabelVisible = items.Count == 0;
        DownloadListEnabled = !inGameAndNotPaused;

        if (inGameAndNotPaused)
        {
            downloadIndicatorSpinIcon.style.rotate = new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle((float)(Time.realtimeSinceStartupAsDouble * LoadingIconSpinDegreesPerSecond % 360d), AngleUnit.Degree)));
        }
    }

    class ListItem
    {
        public readonly string Url;

        public DownloadProgress? DownloadProgress { get; set; }

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

        public string LogText { get; set; } = "";

        public bool ProgressBarErrorState
        {
            get => progressBar.classList.Contains("error");
            set
            {
                if (value && !ProgressBarErrorState)
                    progressBar.AddToClassList("error");
                else if (!value && ProgressBarErrorState)
                    progressBar.RemoveFromClassList("error");
            }
        }

        public bool LogButtonEnabled
        {
            get => logButton.classList.Contains("enabled");
            set
            {
                if (value && !LogButtonEnabled)
                    logButton.AddToClassList("enabled");
                else if (!value && LogButtonEnabled)
                    logButton.RemoveFromClassList("enabled");
            }
        }

        public bool DownloadButtonEnabled
        {
            get => downloadButton.classList.Contains("enabled");
            set
            {
                if (value && !DownloadButtonEnabled)
                    downloadButton.AddToClassList("enabled");
                else if (!value && DownloadButtonEnabled)
                    downloadButton.RemoveFromClassList("enabled");
            }
        }

        public bool RemoveButtonEnabled
        {
            get => removeButton.classList.Contains("enabled");
            set
            {
                if (value && !RemoveButtonEnabled)
                    removeButton.AddToClassList("enabled");
                else if (!value && RemoveButtonEnabled)
                    removeButton.RemoveFromClassList("enabled");
            }
        }

        public VisualElement Element { get; private set; }

        private readonly YtDlpUiManager parent;
        private Label name;
        private Label state;
        private ProgressBar progressBar;
        private Button logButton;
        private Button downloadButton;
        private Button removeButton;

        public ListItem(YtDlpUiManager parent, VisualElement element, string url)
        {
            this.parent = parent;
            Element = element;
            Url = url;

            name = element.Query<Label>(name: "Name").First() ?? throw new InvalidOperationException("Could not find name ui element");
            state = element.Query<Label>(name: "State").First() ?? throw new InvalidOperationException("Could not find state ui element");
            progressBar = element.Query<ProgressBar>(name: "ProgressBar").First() ?? throw new InvalidOperationException("Could not find progress bar ui element");
            progressBar.value = 0f;

            logButton = element.Query<Button>(name: "LogButton").First() ?? throw new InvalidOperationException("Could not find remove button ui element");
            logButton.RegisterCallback<ClickEvent>(OnLogButtonClicked);
            LogButtonEnabled = false;

            downloadButton = element.Query<Button>(name: "DownloadButton").First() ?? throw new InvalidOperationException("Could not find download button ui element");
            downloadButton.RegisterCallback<ClickEvent>(OnDownloadButtonClicked);
            DownloadButtonEnabled = false;

            removeButton = element.Query<Button>(name: "RemoveButton").First() ?? throw new InvalidOperationException("Could not find remove button ui element");
            removeButton.RegisterCallback<ClickEvent>(OnRemoveButtonClicked);
            RemoveButtonEnabled = false;
        }

        private void OnLogButtonClicked(ClickEvent evt)
        {
            if (!logButton.enabledSelf)
                return;

            Modal.Instance.ShowModal(title: "Error Log", message: LogText.Trim(), context: Element.GetRoot());
        }

        private void OnDownloadButtonClicked(ClickEvent evt)
        {
            if (!downloadButton.enabledSelf)
                return;

            LogText = "";
            ProgressBarErrorState = false;
            StateText = "";
            _ = YtDlpManager.Instance.DownloadAudioFile(Url);
        }

        private void OnRemoveButtonClicked(ClickEvent evt)
        {
            if (!removeButton.enabledSelf)
                return;

            parent.itemsContainer.Remove(Element);
            parent.items.Remove(Url);
        }
    }
}
