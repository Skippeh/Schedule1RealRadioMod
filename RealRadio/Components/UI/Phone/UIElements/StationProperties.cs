using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using RealRadio.Components.API.Data;
using RealRadio.Components.Radio;
using RealRadio.Components.YoutubeDL;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.Phone.UIElements;

public class StationProperties
{
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

        saveButton = root.Query<Button>(name: "SaveButton").First() ?? throw new InvalidOperationException("Could not find save button ui element");
        readOnlyChanged += () => saveButton.SetEnabled(!ReadOnly);
        saveButton.RegisterCallback<ClickEvent>(OnSaveButtonClicked);

        deleteButton = root.Query<Button>(name: "DeleteButton").First() ?? throw new InvalidOperationException("Could not find delete button ui element");
        readOnlyChanged += () => deleteButton.SetEnabled(!ReadOnly && !IsNew);
        isNewChanged += () => deleteButton.SetEnabled(!ReadOnly && !IsNew);
        deleteButton.RegisterCallback<ClickEvent>(OnDeleteButtonClicked);
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

        void OnKeyUp(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Delete)
                return;

            var selectedUrls = urlsList.selectedIndices.Select(index => (string)urlsList.itemsSource[index]).ToList();

            if (selectedUrls.Count == 0)
                return;

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

            void OnConfirm(ref bool preventClose)
            {
                foreach (var url in selectedUrls)
                    stationUrls.Remove(url);

                urlsList.selectedIndex = -1;
                urlsList.Rebuild();
            }
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

        parent.StationDeleteRequested?.Invoke(station);
    }
}
