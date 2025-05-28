using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RealRadio.Components.Radio;
using RealRadio.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.Phone.UIElements;

[RequireComponent(typeof(UIDocument))]
public class RadioAppUi : MonoBehaviour
{
    public const float ScrollSpeed = 100;

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
        stationProperties = new StationProperties(root.Query<VisualElement>(name: "StationProperties").First() ?? throw new InvalidOperationException("Could not find station properties ui element"));

        InitializeStationList();

        newStationButton.RegisterCallback<ClickEvent>(OnNewStationButtonClicked);
        RadioStationManager.Instance.OnStationsChanged += OnStationsChanged;

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
            var station = RadioStationManager.Instance.SortedStations[index];
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
                stationProperties.ReadOnly = station != null && RadioStationManager.Instance.GetStationSource(station) is not StationSource.UserCreated or null;
            }
        };

        stationList.itemsSource = RadioStationManager.Instance.SortedStations;
    }

    private void OnStationsChanged()
    {
        stationList.Rebuild();
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
    }
}
