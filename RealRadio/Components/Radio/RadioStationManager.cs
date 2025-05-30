using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HashUtility;
using NAudio.SoundFont;
using RealRadio.Data;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
using UnityEngine;

namespace RealRadio.Components.Radio;

public class RadioStationManager : PersistentSingleton<RadioStationManager>
{
    public Action<RadioStation>? StationAdded;
    public Action<RadioStation>? StationRemoved;
    public Action? OnStationsChanged;
    public ReadOnlyDictionary<uint, RadioStation> StationsByHashedId { get; private set; }
    public ReadOnlyCollection<RadioStation> Stations { get; private set; }
    public ReadOnlyCollection<RadioStation> SortedStations { get; private set; }

    private List<RadioStation> stations = [];
    private List<RadioStation> sortedStations = [];
    private Dictionary<uint, RadioStation> stationsByHashedId = [];
    private Dictionary<uint, RadioStation> npcStations = [];
    private Dictionary<uint, StationSource> stationSources = [];
    private bool stationsChanged;

    public RadioStationManager()
    {
        StationsByHashedId = new ReadOnlyDictionary<uint, RadioStation>(stationsByHashedId);
        Stations = new ReadOnlyCollection<RadioStation>(stations);
        SortedStations = new ReadOnlyCollection<RadioStation>(sortedStations);
    }

    public override void Awake()
    {
        base.Awake();

        if (Plugin.Assets?.DefaultRadioStations != null)
        {
            foreach (var station in Plugin.Assets.DefaultRadioStations)
            {
                AddRadioStation(station, StationSource.DefaultStation);
            }

            InternalOnStationsChanged();
        }
    }

    public void AddRadioStation(RadioStation station, StationSource source)
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
        stationsByHashedId[hashedId] = station;
        stationSources[hashedId] = source;

        if (station.CanBePlayedByNPCs)
            npcStations[hashedId] = station;

        stationsChanged = true;
        StationAdded?.Invoke(station);
    }

    public void RemoveRadioStation(RadioStation station)
    {
        if (station.Id == null)
            throw new ArgumentNullException(nameof(station.Id));

        uint hashedId = station.Id.GetStableHashCode();
        npcStations.Remove(hashedId);
        stations.Remove(station);
        stationsByHashedId.Remove(hashedId);
        stationSources.Remove(hashedId);
        stationsChanged = true;
        StationRemoved?.Invoke(station);
    }

    public int GetRandomNPCStationIndex()
    {
        var index = UnityEngine.Random.Range(0, npcStations.Count);
        var station = npcStations.ElementAt(index).Value;
        return stations.IndexOf(station);
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
        sortedStations.Clear();
        sortedStations.AddRange(stations.OrderBy(s => s.Name));
    }

    /// <summary>
    /// Returns the index of the sorted station in the list of unsorted stations, or -1 if the index is out of range.
    /// </summary>
    public int IndexOfSortedStation(int sortedIndex)
    {
        if (sortedIndex < 0 || sortedIndex >= SortedStations.Count)
            return -1;

        return stations.IndexOf(SortedStations[sortedIndex]);
    }

    /// <summary>
    /// Returns the index of the unsorted station in the list of sorted stations, or -1 if the index is out of range.
    /// </summary>
    public int IndexOfUnsortedStation(int unsortedIndex)
    {
        if (unsortedIndex < 0 || unsortedIndex >= stations.Count)
            return -1;

        return sortedStations.IndexOf(stations[unsortedIndex]);
    }

    public StationSource? GetStationSource(RadioStation station)
    {
        if (station == null)
            throw new ArgumentNullException(nameof(station));

        return GetStationSource(station.Id);
    }

    public StationSource? GetStationSource(string? stationId)
    {
        if (stationId == null)
            return null;

        if (!stationSources.TryGetValue(stationId.GetStableHashCode(), out var source))
            return null;

        return source;
    }
}
