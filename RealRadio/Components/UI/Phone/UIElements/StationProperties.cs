using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using AngleSharp.Html.Dom;
using RealRadio.Components.API.Data;
using RealRadio.Components.Radio;
using RealRadio.Components.YoutubeDL;
using ScheduleOne.Tiles;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.Phone.UIElements;

public class StationProperties
{
    public VisualElement Element => root;

    public Action<RadioStation?>? StationChanged;

    public RadioStation? Station
    {
        get => station;
        set
        {
            if (station == value)
                return;

            station = value;
            stationChanged?.Invoke();
        }
    }

    public bool ReadOnly
    {
        get => readOnly;
        set
        {
            if (readOnly == value)
                return;

            readOnly = value;
            readOnlyChanged?.Invoke();
        }
    }

    public bool IsNew
    {
        get => isNew;
        set
        {
            if (isNew == value)
                return;

            isNew = value;
            isNewChanged?.Invoke();
        }
    }

    private ScrollView fieldsScrollView;
    private TextField nameField;
    private EnumField typeField;
    private TextField abbreviationField;
    private Toggle canBePlayedByNPCsToggle;
    private TextField textColorField;
    private TextField backgroundColorField;
    private Toggle roundedBackgroundToggle;
    private TextField urlField;
    private VisualElement urlsContainer;
    private ListView urlsList;
    private Button saveButton;
    private Button deleteButton;
    private Button addUrlButton;
    private Button importPlaylistButton;
    private Button deleteUrlButton;

    private RadioStation? station;
    private List<string> stationUrls = [];
    private bool readOnly;
    private bool isNew = true;
    private readonly RadioAppUi parent;
    private readonly VisualElement root;
    private Action? stationChanged;
    private Action? readOnlyChanged;
    private Action? isNewChanged;

    public StationProperties(RadioAppUi parent, VisualElement root)
    {
        this.parent = parent;
        this.root = root;

        stationChanged += () => StationChanged?.Invoke(station);
        UserStationsManager.Instance.StationUpdated += OnStationUpdated;

        fieldsScrollView = root.Query<ScrollView>(name: "FieldsScrollView").First() ?? throw new InvalidOperationException("Could not find fields ScrollView ui element");
        fieldsScrollView.mouseWheelScrollSize = RadioAppUi.ScrollSpeed;

        nameField = root.Query<TextField>(name: "Name").First() ?? throw new InvalidOperationException("Could not find name TextField ui element");
        stationChanged += () => nameField.text = Station?.Name ?? string.Empty;
        readOnlyChanged += () => nameField.SetEnabled(!ReadOnly);

        typeField = root.Query<EnumField>(name: "Type").First() ?? throw new InvalidOperationException("Could not find type EnumField ui element");
        stationChanged += () => typeField.value = Station?.Type ?? 0;
        readOnlyChanged += () => typeField.SetEnabled(!ReadOnly && IsNew);
        isNewChanged += () => typeField.SetEnabled(!ReadOnly && IsNew);
        typeField.RegisterValueChangedCallback((_) => OnTypeChanged());

        abbreviationField = root.Query<TextField>(name: "Abbreviation").First() ?? throw new InvalidOperationException("Could not find abbreviation TextField ui element");
        stationChanged += () => abbreviationField.text = Station?.Abbreviation ?? string.Empty;
        readOnlyChanged += () => abbreviationField.SetEnabled(!ReadOnly);

        canBePlayedByNPCsToggle = root.Query<Toggle>(name: "CanBePlayedByNPCs").First() ?? throw new InvalidOperationException("Could not find can be played by NPCs Toggle ui element");
        stationChanged += () => canBePlayedByNPCsToggle.value = Station?.CanBePlayedByNPCs ?? false;
        readOnlyChanged += () => canBePlayedByNPCsToggle.SetEnabled(!ReadOnly);

        textColorField = root.Query<TextField>(name: "TextColor").First() ?? throw new InvalidOperationException("Could not find text color TextField ui element");
        stationChanged += () => textColorField.text = Station?.TextColor ?? string.Empty;
        readOnlyChanged += () => textColorField.SetEnabled(!ReadOnly);

        backgroundColorField = root.Query<TextField>(name: "BackgroundColor").First() ?? throw new InvalidOperationException("Could not find background color TextField ui element");
        stationChanged += () => backgroundColorField.text = Station?.BackgroundColor ?? string.Empty;
        readOnlyChanged += () => backgroundColorField.SetEnabled(!ReadOnly);

        roundedBackgroundToggle = root.Query<Toggle>(name: "RoundedBackground").First() ?? throw new InvalidOperationException("Could not find rounded background Toggle ui element");
        stationChanged += () => roundedBackgroundToggle.value = Station?.RoundedBackground ?? false;
        readOnlyChanged += () => roundedBackgroundToggle.SetEnabled(!ReadOnly);

        urlField = root.Query<TextField>(name: "Url").First() ?? throw new InvalidOperationException("Could not find url TextField ui element");
        stationChanged += () =>
        {
            urlField.text = Station?.Url ?? string.Empty;
            urlField.style.display = Station?.Type == RadioType.InternetRadio ? DisplayStyle.Flex : DisplayStyle.None;
        };
        readOnlyChanged += () => urlField.SetEnabled(!ReadOnly);

        urlsContainer = root.Query(name: "UrlsContainer").First() ?? throw new InvalidOperationException("Could not find urls container ui element");
        urlsList = urlsContainer.Query<ListView>(name: "UrlsList").First() ?? throw new InvalidOperationException("Could not find urls list ui element");
        urlsList.scrollView.mouseWheelScrollSize = RadioAppUi.ScrollSpeed;
        InitializeUrlsList();
        stationChanged += () =>
        {
            stationUrls = Station?.Urls?.ToList() ?? [];
            urlsList.itemsSource = stationUrls;
            urlsList.selectedIndex = -1;
            urlsList.Rebuild();
            urlsContainer.style.display = Station?.Type == RadioType.YtDlp ? DisplayStyle.Flex : DisplayStyle.None;
        };

        addUrlButton = root.Query<Button>(name: "AddUrlButton").First() ?? throw new InvalidOperationException("Could not find add url button ui element");
        readOnlyChanged += () => addUrlButton.SetEnabled(!ReadOnly);
        addUrlButton.RegisterCallback<ClickEvent>(OnAddUrlButtonClicked);

        importPlaylistButton = root.Query<Button>(name: "ImportPlaylistButton").First() ?? throw new InvalidOperationException("Could not find import playlist button ui element");
        readOnlyChanged += () => importPlaylistButton.SetEnabled(!ReadOnly);
        importPlaylistButton.RegisterCallback<ClickEvent>(OnImportPlaylistButtonClicked);

        deleteUrlButton = root.Query<Button>(name: "DeleteUrlButton").First() ?? throw new InvalidOperationException("Could not find delete url button ui element");
        readOnlyChanged += () => deleteUrlButton.SetEnabled(!ReadOnly);
        deleteUrlButton.RegisterCallback<ClickEvent>(OnDeleteUrlButtonClicked);

        saveButton = root.Query<Button>(name: "SaveButton").First() ?? throw new InvalidOperationException("Could not find save button ui element");
        readOnlyChanged += () => saveButton.SetEnabled(!ReadOnly);
        saveButton.RegisterCallback<ClickEvent>(OnSaveButtonClicked);

        deleteButton = root.Query<Button>(name: "DeleteButton").First() ?? throw new InvalidOperationException("Could not find delete button ui element");
        readOnlyChanged += () => deleteButton.SetEnabled(!ReadOnly && !IsNew);
        isNewChanged += () => deleteButton.SetEnabled(!ReadOnly && !IsNew);
        deleteButton.RegisterCallback<ClickEvent>(OnDeleteButtonClicked);

        RefreshUI();
    }

    private void RefreshUI()
    {
        isNewChanged?.Invoke();
        stationChanged?.Invoke();
        readOnlyChanged?.Invoke();
    }

    private void InitializeUrlsList()
    {
        urlsList.makeItem += () =>
        {
            var item = new UrlListItem(parent.UrlListItemAsset).Element;
            return item;
        };

        urlsList.bindItem += (element, index) =>
        {
            var url = (string)urlsList.itemsSource[index];
            var listItem = (UrlListItem)element.userData;
            listItem.Url = url;

            parent.StartCoroutine(FetchMetaData());

            IEnumerator FetchMetaData()
            {
                var metaDataTask = YtDlpManager.Instance.FetchMetaData(url);
                yield return new WaitUntil(() => metaDataTask.IsCompleted);

                if (metaDataTask.IsFaulted)
                {
                    Plugin.Logger.LogError($"Failed to fetch metadata for URL '{url}':\n{metaDataTask.Exception}");
                    yield break;
                }

                listItem.HumanReadableText = RadioStationInfoManager.SongInfoFromVideoData(metaDataTask.Result).ToString();
            }
        };

        urlsList.RegisterCallback<KeyUpEvent>(OnKeyUp, TrickleDown.TrickleDown);
        urlsList.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);

        void OnKeyUp(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Delete)
                return;

            var selectedUrls = urlsList.selectedIndices.Select(index => (string)urlsList.itemsSource[index]).ToList();

            if (selectedUrls.Count == 0)
                return;

            ConfirmDeleteUrls(selectedUrls);
        }

        void OnClick(ClickEvent evt)
        {
            if (evt.button != 0)
                return;

            if (evt.clickCount != 2)
                return;

            if (urlsList.selectedIndex == -1)
                return;

            string url = (string)urlsList.itemsSource[urlsList.selectedIndex];

            Plugin.Logger.LogInfo($"Opening edit modal for '{url}'");

            OpenUrlEditModal(url, (newUrl) =>
            {
                var index = urlsList.itemsSource.IndexOf(url);

                if (index >= 0)
                    urlsList.itemsSource[index] = newUrl;

                urlsList.Rebuild();
            });
        }
    }

    private void OnAddUrlButtonClicked(ClickEvent evt)
    {
        if (evt.button != 0)
            return;

        OpenUrlEditModal(string.Empty, (url) =>
        {
            stationUrls.Add(url);
            urlsList.Rebuild();
        });
    }

    private void OnImportPlaylistButtonClicked(ClickEvent evt)
    {
        if (evt.button != 0)
            return;

        OpenImportPlaylistModal((urls) =>
        {
            stationUrls.Capacity = Math.Max(stationUrls.Capacity, stationUrls.Count + urls.Count);
            foreach (string url in urls)
            {
                if (!stationUrls.Contains(url))
                    stationUrls.Add(url);
            }

            urlsList.Rebuild();
        });
    }

    private void OnDeleteUrlButtonClicked(ClickEvent evt)
    {
        if (evt.button != 0)
            return;

        var selectedUrls = urlsList.selectedIndices.Select(index => (string)urlsList.itemsSource[index]).ToList();

        if (selectedUrls.Count == 0)
            return;

        ConfirmDeleteUrls(selectedUrls);
    }

    private ModalInstance OpenUrlEditModal(string url, Action<string> onSaveUrl)
    {
        UrlEditModal modal = null!;
        bool isNew = string.IsNullOrEmpty(url);
        return Modal.Instance.ShowModal(parent.UrlEditModalAsset, SetupContent, root, title: isNew ? "Add song" : "Edit song", confirmText: isNew ? "Add" : "Update", cancelText: "Cancel", onConfirm: OnConfirm);

        void SetupContent(ModalInstance instance)
        {
            modal = new UrlEditModal(parent, instance.Content, url);
        }

        void OnConfirm(ModalInstance instance, ref bool preventClose)
        {
            if (!modal.IsValid() || modal.State != UrlEditModal.UrlState.ValidAndMetaDataLoaded)
            {
                preventClose = true;
                return;
            }

            onSaveUrl(modal.Url);
        }
    }

    private ModalInstance OpenImportPlaylistModal(Action<ICollection<string>> onImportUrls)
    {
        ImportPlaylistModal importModal = null!;

        bool validated = false;
        var importModalInstance = Modal.Instance.ShowModal(parent.ImportPlaylistModalAsset, SetupContent, root, title: "Import playlist", confirmText: "Import", cancelText: "Cancel", onConfirm: OnConfirm, onClosed: OnClosed);
        return importModalInstance;

        void SetupContent(ModalInstance instance)
        {
            importModal = new ImportPlaylistModal(parent, instance.Content, parent.UrlListItemAsset);
        }

        void OnConfirm(ModalInstance outerModal, ref bool preventClose)
        {
            if (validated)
                return;

            preventClose = true;

            if (!importModal.IsValid())
            {
                return;
            }

            ValidatePlaylistModal validateModal = null!;
            Modal.Instance.ShowModal(parent.ValidatePlaylistModalAsset, SetupValidateModal, root, title: "Validating playlists...", cancelText: "Cancel", onClosed: OnValidateModalClosed);

            void SetupValidateModal(ModalInstance innerModal)
            {
                validateModal = new ValidatePlaylistModal(parent, innerModal.Content, importModal.SongUrls);

                validateModal.OnValidated += (urls) =>
                {
                    validated = true;
                    outerModal.Close(confirmed: true);
                    innerModal.Close(confirmed: true);
                    onImportUrls(urls);
                };
            }

            void OnValidateModalClosed(ModalInstance instance)
            {
                validateModal.Dispose();
            }
        }

        void OnClosed(ModalInstance instance)
        {
            importModal.Dispose();
        }
    }

    [return: NotNullIfNotNull(nameof(color))]
    private static string? GetColorString(Color? color)
    {
        if (color == null)
            return null;

        byte r, g, b, a;

        r = (byte)(color.Value.r * 255);
        g = (byte)(color.Value.g * 255);
        b = (byte)(color.Value.b * 255);
        a = (byte)(color.Value.a * 255);

        string result = $"#{r:X2}{g:X2}{b:X2}";

        if (a != 255)
            result += $"{a:X2}";

        return result;
    }

    private void OnStationUpdated(RadioStation station, bool isNew)
    {
        if (station.Id != this.station?.Id)
            return;

        Station = station;
    }

    private void OnTypeChanged()
    {
        switch (typeField.value)
        {
            case RadioType.InternetRadio:
                urlsContainer.style.display = DisplayStyle.None;
                urlField.style.display = DisplayStyle.Flex;
                break;
            case RadioType.YtDlp:
                urlsContainer.style.display = DisplayStyle.Flex;
                urlField.style.display = DisplayStyle.None;
                break;
            default:
                Plugin.Logger.LogWarning($"Unknown radio type selected: {typeField.value}");
                break;
        }
    }

    private void OnSaveButtonClicked(ClickEvent evt)
    {
        if (station == null)
            return;

        var newStation = new RadioStation
        {
            Id = station.Id,
            Name = nameField.text.Trim(),
            Abbreviation = abbreviationField.text.Trim(),
            Type = typeField.value as RadioType? ?? throw new InvalidOperationException("Could not convert radio type to enum value"),
            CanBePlayedByNPCs = canBePlayedByNPCsToggle.value,
            TextColor = textColorField.text.Trim(),
            BackgroundColor = backgroundColorField.text.Trim(),
            RoundedBackground = roundedBackgroundToggle.value,
            Url = urlField.text.Trim(),
            Urls = GetUrls(),
        };

        if (!newStation.IsValid(out var invalidReasons))
        {
            Modal.Instance.ShowModal(title: "Validation failed", message: $"- {string.Join("\n- ", invalidReasons)}", context: root, confirmText: "OK");
            return;
        }

        parent.StationSaveRequested?.Invoke(newStation);

        return;

        string[] GetUrls()
        {
            if (urlsList.itemsSource == null)
                return [];

            List<string> result = new(capacity: urlsList.itemsSource.Count);

            foreach (var item in urlsList.itemsSource)
            {
                if (item is string url)
                    result.Add(url.Trim());
            }

            return result.ToArray();
        }
    }

    private void OnDeleteButtonClicked(ClickEvent evt)
    {
        if (station == null)
            return;

        Modal.Instance.ShowModal("Delete station", $"Are you sure you want to delete this station?\n\n{station.Name}", root, "Yes", "No", OnConfirm);

        void OnConfirm(ModalInstance instance, ref bool preventClose)
        {
            parent.StationDeleteRequested?.Invoke(station);
        }
    }

    private void ConfirmDeleteUrls(ICollection<string> selectedUrls)
    {
        string pluralText = selectedUrls.Count == 1 ? "this song" : "these songs";
        var builder = new StringBuilder($"Are you sure you want to remove {pluralText} from the playlist?\n\n");

        foreach (var url in selectedUrls)
        {
            YtDlpManager.Instance.AudioMetaData.TryGetValue(url, out var metaData);

            if (metaData != null)
                builder.AppendLine(RadioStationInfoManager.SongInfoFromVideoData(metaData).ToString());
            else
                builder.AppendLine(url);
        }

        pluralText = selectedUrls.Count == 1 ? "song" : "songs";
        var modal = Modal.Instance.ShowModal($"Delete {pluralText}", builder.ToString(), root, "Yes", "No", onConfirm: OnConfirm);

        void OnConfirm(ModalInstance instance, ref bool preventClose)
        {
            foreach (var url in selectedUrls)
                stationUrls.Remove(url);

            urlsList.selectedIndex = -1;
            urlsList.Rebuild();
        }
    }
}
