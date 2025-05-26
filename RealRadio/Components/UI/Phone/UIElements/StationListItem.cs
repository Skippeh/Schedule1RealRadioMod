using System;
using RealRadio.Data;
using UnityEngine.UIElements;

namespace RealRadio.Components.UI.Phone.UIElements;

public class StationListItem
{
    public RadioStation? Station => station;

    public VisualElement Element { get; private set; }

    private RadioStation? station;

    private Label stationNameLabel;

    public StationListItem(VisualTreeAsset listItemAsset)
    {
        Element = listItemAsset.Instantiate();
        Element.userData = this;

        stationNameLabel = Element.Query<Label>(name: "Name").First() ?? throw new InvalidOperationException("Could not find name label ui element");
    }

    public void SetStation(RadioStation station)
    {
        if (station == null)
            throw new ArgumentNullException(nameof(station));

        this.station = station;
        OnStationSet();
    }

    private void OnStationSet()
    {
        if (station == null)
        {
            Plugin.Logger.LogInfo("Station is null in OnStationSet call");
            return;
        }

        stationNameLabel.text = station.Name;
    }
}
