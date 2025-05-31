using System;
using FishNet.Object;
using HashUtility;
using RealRadio.Components.Radio;
using RealRadio.Components.UI.WorldUI;
using ScheduleOne;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RealRadio.Components.Building.Buildables;

public class AnalogRadio : Radio
{
    [SerializeField] private DraggableSlider stationEditSlider = null!;
    [SerializeField] private DraggableSlider volumeEditSlider = null!;

    public override void Awake()
    {
        base.Awake();

        if (stationEditSlider == null)
            throw new InvalidOperationException("StationEditSlider is null");

        if (volumeEditSlider == null)
            throw new InvalidOperationException("VolumeEditSlider is null");

        stationEditSlider.LoopedAround.AddListener(OnSliderLoopedAround);
        volumeEditSlider.ValueChanged.AddListener(OnVolumeSliderValueChanged);
    }

    public override void Start()
    {
        base.Start();

        if (isGhost)
            return;

        volumeEditSlider.Value = Volume * volumeEditSlider.MaxValue;
    }

    private void OnSliderLoopedAround(float direction)
    {
        var indexOfStation = RadioStation == null ? 0 : RadioStationManager.Instance.SortedStations.IndexOf(RadioStation);

        int nextIndex = indexOfStation + (int)direction;
        nextIndex = (nextIndex + RadioStationManager.Instance.SortedStations.Count) % RadioStationManager.Instance.SortedStations.Count;

        if (nextIndex < 0)
            nextIndex += RadioStationManager.Instance.SortedStations.Count;

        var nextStation = RadioStationManager.Instance.SortedStations[nextIndex];
        SetRadioStationIdHash(nextStation.Id!.GetStableHashCode());
    }

    private void OnVolumeSliderValueChanged(float value)
    {
        Volume = value / volumeEditSlider.MaxValue;

        if (value <= 0 && IsOn)
            IsOn = false;
        else if (value > 0 && !IsOn)
            IsOn = true;
    }

    protected override void OnPlayerUserChanged(NetworkObject prev, NetworkObject next, bool asServer)
    {
        base.OnPlayerUserChanged(prev, next, asServer);

        if (!asServer)
        {
            stationEditSlider.gameObject.SetActive(next == Player.Local.NetworkObject);
        }
    }
}
