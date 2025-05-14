using System;
using System.Collections.Generic;
using System.Linq;
using HashUtility;
using RealRadio.Data;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
using UnityEngine;

namespace RealRadio.Components.Radio;

public class RadioStationManager : PersistentSingleton<RadioStationManager>
{
    public Action<RadioStation>? StationAdded;
    public Action? OnStationsChanged;
    public Dictionary<uint, RadioStation> StationsByHashedId { get; private set; } = [];
    public IReadOnlyList<RadioStation> Stations => stations;
    public IReadOnlyList<RadioStation> SortedStations { get; private set; } = null!;

    private List<RadioStation> stations = [];
    private Dictionary<int, RadioStation> npcStations = [];
    private bool stationsChanged;

    public override void Awake()
    {
        base.Awake();

        if (Plugin.Assets?.DefaultRadioStations != null)
        {
            foreach (var station in Plugin.Assets.DefaultRadioStations)
            {
                AddRadioStation(station);
            }

            InternalOnStationsChanged();
        }
    }

    public void AddRadioStation(RadioStation station)
    {
        if (station.Id == null)
            throw new ArgumentNullException(nameof(station.Id));

        uint hashedId = station.Id.GetStableHashCode();

        if (StationsByHashedId.ContainsKey(hashedId))
        {
            Plugin.Logger.LogWarning($"Radio station with ID '{station.Id}' already exists");
            return;
        }

        stations.Add(station);
        StationsByHashedId[hashedId] = station;

        if (station.CanBePlayedByNPCs)
            npcStations.Add(stations.Count - 1, station);

        stationsChanged = true;
        StationAdded?.Invoke(station);
    }

    public void RemoveRadioStation(RadioStation station)
    {
        if (station.Id == null)
            throw new ArgumentNullException(nameof(station.Id));

        npcStations.Remove(stations.IndexOf(station));
        stations.Remove(station);
        StationsByHashedId.Remove(station.Id.GetStableHashCode());
        stationsChanged = true;
    }

    public int GetRandomNPCStationIndex()
    {
        int index = UnityEngine.Random.Range(0, npcStations.Count);
        return npcStations.ElementAt(index).Key;
    }

    private void LateUpdate()
    {
        if (stationsChanged)
        {
            stationsChanged = false;
            InternalOnStationsChanged();
        }
    }

    private void InternalOnStationsChanged()
    {
        UpdateSortedStations();
        OnStationsChanged?.Invoke();
    }

    private void UpdateSortedStations()
    {
        SortedStations = [.. Stations.OrderBy(x => x.Name)];
    }
}
