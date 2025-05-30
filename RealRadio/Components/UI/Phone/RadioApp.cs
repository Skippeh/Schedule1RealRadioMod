using System;
using HashUtility;
using RealRadio.Components.API.Data;
using RealRadio.Components.Radio;
using RealRadio.Components.UI.Phone.UIElements;
using UnityEngine;

namespace RealRadio.Components.UI.Phone;

public class RadioApp : UITKApp<RadioApp>
{
    [Header("References")]
    [SerializeField]
    private RadioAppUi ui = null!;

    public override void Awake()
    {
        base.Awake();

        if (ui == null)
            throw new InvalidOperationException("UI is null");

        ui.StationSaveRequested += OnStationSaveRequested;
        ui.StationDeleteRequested += OnStationDeleteRequested;
    }

    private void OnStationDeleteRequested(RadioStation station)
    {
        if (station == null)
            throw new ArgumentNullException(nameof(station));

        if (station.Id == null)
            throw new ArgumentNullException(nameof(station.Id), "Station id cannot be null");

        UserStationsManager.Instance.RequestRemoveStation(station.Id.GetStableHashCode());
    }

    private void OnStationSaveRequested(RadioStation station)
    {
        if (station == null)
            throw new ArgumentNullException(nameof(station));

        if (station.Id == null)
            throw new ArgumentNullException(nameof(station.Id), "Station id cannot be null");

        UserStationsManager.Instance.RequestAddOrUpdateStation(station);
    }
}
