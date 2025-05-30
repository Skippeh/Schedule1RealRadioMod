using System;
using System.Linq;
using RealRadio.Components.API.Data;
using RealRadio.Components.Radio;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.Phone.UIElements;

[RequireComponent(typeof(UIDocument))]
public class RadioAppUi : MonoBehaviour
{
    public const float ScrollSpeed = 100;

    public Action<RadioStation>? StationSaveRequested { get; set; }
    public Action<RadioStation>? StationDeleteRequested { get; set; }

    [Header("Asset References")]
    [SerializeField]
    private VisualTreeAsset stationListItemAsset = null!;

    [Header("Style")]
    [SerializeField]
    private float backgroundScrollSpeed;

    private Vector2 backgroundMoveDirection;

    private UIDocument document = null!;
    private VisualElement root = null!;

    private Button newStationButton = null!;
    private ListView stationList = null!;
    private StationProperties? stationProperties;

    private void Awake()
    {
        document = GetComponent<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object");

        if (stationListItemAsset == null)
            throw new InvalidOperationException("StationListItemAsset is null");
    }

    void OnEnable()
    {
        root = document.rootVisualElement.Query(name: "Root").First() ?? throw new InvalidOperationException("Could not find root ui element");
        newStationButton = root.Query<Button>(name: "NewStationButton").First() ?? throw new InvalidOperationException("Could not find new station button ui element");
        stationList = root.Query<ListView>(name: "StationList").First() ?? throw new InvalidOperationException("Could not find station list ui element");
        stationProperties = new StationProperties(this, root.Query<VisualElement>(name: "StationProperties").First() ?? throw new InvalidOperationException("Could not find station properties ui element"));

        InitializeStationList();

        newStationButton.RegisterCallback<ClickEvent>(OnNewStationButtonClicked);
        UserStationsManager.instance.StationsChanged += OnStationsChanged;

        RandomizeBackgroundParameters();
    }

    void OnDisable()
    {
        RadioStationManager.Instance.OnStationsChanged -= OnStationsChanged;
        stationProperties = null;
    }

    private void InitializeStationList()
    {
        stationList.scrollView.mouseWheelScrollSize = ScrollSpeed;

        stationList.makeItem = () =>
        {
            return new StationListItem(stationListItemAsset).Element;
        };

        stationList.bindItem = (element, index) =>
        {
            var station = (RadioStation)stationList.itemsSource[index];
            var listItem = element.userData as StationListItem;
            listItem?.SetStation(station);
        };

        stationList.selectedIndicesChanged += (selectedIndices) =>
        {
            var indices = selectedIndices.ToList();
            int index = indices.Count > 0 ? indices[0] : -1;

            if (index < 0)
                return;

            var station = stationList.itemsSource[index] as RadioStation;

            if (stationProperties != null)
            {
                stationProperties.Station = station;
                stationProperties.ReadOnly = false;
                stationProperties.IsNew = false;
            }
        };

        stationProperties!.StationChanged += (station) =>
        {
            var index = stationList.itemsSource.IndexOf(station);
            stationList.selectedIndex = index;
        };

        OnStationsChanged();
    }

    private void OnStationsChanged()
    {
        stationList.itemsSource = UserStationsManager.Instance.Stations.Values.OrderBy(s => s.Name).ToList();

        if (stationProperties != null)
        {
            var index = stationList.itemsSource.IndexOf(stationProperties.Station);
            stationList.selectedIndex = index;
        }
    }

    private void RandomizeBackgroundParameters()
    {
        root.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, UnityEngine.Random.Range(-1000f, 1000f));
        root.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, UnityEngine.Random.Range(-1000f, 1000f));

        float direction = UnityEngine.Random.Range(0f, 360f);
        backgroundMoveDirection = new Vector2(Mathf.Cos(direction * Mathf.Deg2Rad), Mathf.Sin(direction * Mathf.Deg2Rad)).normalized;
    }

    private void Update()
    {
        UpdateBackgroundPosition();
    }

    private void UpdateBackgroundPosition()
    {
        float x = root.resolvedStyle.backgroundPositionX.offset.m_Value;
        float y = root.resolvedStyle.backgroundPositionY.offset.m_Value;

        root.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, x + (backgroundScrollSpeed * Time.unscaledDeltaTime * backgroundMoveDirection.x));
        root.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, y + (backgroundScrollSpeed * Time.unscaledDeltaTime * backgroundMoveDirection.y));
    }

    private void OnNewStationButtonClicked(ClickEvent evt)
    {
        if (stationProperties == null)
            return;

        var station = new RadioStation
        {
            Id = Guid.NewGuid().ToString(),
            Type = RadioType.YtDlp
        };

        stationProperties.Station = station;
        stationProperties.IsNew = true;
        stationProperties.ReadOnly = false;
    }
}
