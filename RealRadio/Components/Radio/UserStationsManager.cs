using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using HashUtility;
using RealRadio.Components.API.Data;
using RealRadio.Peristence.Data;
using RealRadio.Persistence.Loaders;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Loaders;

namespace RealRadio.Components.Radio;

public class UserStationsManager : NetworkSingleton<UserStationsManager>, IBaseSaveable
{
    public delegate void OnStationUpdatedDelegate(RadioStation station, bool isNew);

    public ReadOnlyDictionary<uint, RadioStation> Stations { get; private set; } = null!;

    /// <summary>
    /// Called when a station was removed.
    /// </summary>
    public Action<RadioStation>? StationRemoved { get; set; }

    /// <summary>
    /// Called when a station was added or updated.
    /// </summary>
    public event OnStationUpdatedDelegate? StationUpdated;

    /// <summary>
    /// Called when any amount of stations was added or updated.
    /// </summary>
    public Action? StationsChanged { get; set; }

    public string SaveFolderName => SaveFileName;

    public string SaveFileName => "RealRadioUserStations";

    public Loader Loader => loader;

    public bool ShouldSaveUnderFolder => false;

    public List<string> LocalExtraFiles { get; set; } = [];
    public List<string> LocalExtraFolders { get; set; } = [];

    public bool HasChanged { get; set; }

    private Loader loader = new UserStationsLoader();
    private bool invokeStationsChanged;
    private Dictionary<uint, RadioStation> stations = [];

    public override void Awake()
    {
        base.Awake();
        Stations = new ReadOnlyDictionary<uint, RadioStation>(stations);
        InitializeSaveable();
    }

    public void InitializeSaveable()
    {
        SaveManager.Instance.RegisterSaveable(this);
    }

    public string GetSaveString()
    {
        return new UserStationsData(stations.Values.ToList()).GetJson();
    }

    public void AddOrUpdateStations(IEnumerable<RadioStation> stations)
    {
        foreach (var station in stations)
            AddOrUpdateStation(station);
    }

    public void AddOrUpdateStation(RadioStation station)
    {
        if (station.Id == null)
            throw new ArgumentNullException(nameof(station.Id), "Station id cannot be null");

        uint idHash = station.Id.GetStableHashCode();
        bool updatedExisting = false;

        if (stations.Remove(idHash))
        {
            updatedExisting = true;
        }

        stations.Add(idHash, station);
        StationUpdated?.Invoke(station, isNew: !updatedExisting);

        HasChanged = true;
        invokeStationsChanged = true;

        if (IsServer)
            ReceiveStation(null, station);
    }

    public void RemoveStation(RadioStation station)
    {
        if (station == null)
            throw new ArgumentNullException(nameof(station));

        if (station.Id == null)
            throw new ArgumentNullException(nameof(station.Id));

        RemoveStationByIdHash(station.Id.GetStableHashCode());
    }

    public void RemoveStationById(string id)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        RemoveStationByIdHash(id.GetStableHashCode());
    }

    public void RemoveStationByIdHash(uint idHash)
    {
        if (stations.Remove(idHash, out var station))
        {
            StationRemoved?.Invoke(station);

            if (IsServer)
                ReceiveRemoveStation(idHash);

            HasChanged = true;
            invokeStationsChanged = true;
        }
    }

    void LateUpdate()
    {
        if (invokeStationsChanged)
        {
            invokeStationsChanged = false;
            StationsChanged?.Invoke();
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsClientOnly)
            RequestStations();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestStations(NetworkConnection requester = null!)
    {
        ReceiveStations(requester, [.. stations.Values]);
    }

    [TargetRpc]
    [ObserversRpc(ExcludeServer = true)]
    private void ReceiveStation(NetworkConnection? target, RadioStation station)
    {
        AddOrUpdateStation(station);
    }

    [TargetRpc]
    private void ReceiveStations(NetworkConnection target, RadioStation[] stations)
    {
        AddOrUpdateStations(stations);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void ReceiveRemoveStation(uint idHash)
    {
        RemoveStationByIdHash(idHash);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestAddOrUpdateStation(RadioStation station)
    {
        AddOrUpdateStation(station);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRemoveStation(uint idHash)
    {
        RemoveStationByIdHash(idHash);
    }
}
