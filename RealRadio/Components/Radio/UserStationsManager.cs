using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HashUtility;
using RealRadio.Data;
using RealRadio.Peristence.Data;
using RealRadio.Persistence.Loaders;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Loaders;

namespace RealRadio.Components.Radio;

public class UserStationsManager : NetworkSingleton<UserStationsManager>, IBaseSaveable
{
    private Dictionary<uint, RadioStation> stations = [];

    public string SaveFolderName => SaveFileName;

    public string SaveFileName => "RealRadioUserStations";

    public Loader Loader => loader;

    public bool ShouldSaveUnderFolder => false;

    public List<string> LocalExtraFiles { get; set; } = [];
    public List<string> LocalExtraFolders { get; set; } = [];

    public bool HasChanged { get; set; }

    private Loader loader = new UserStationsLoader();

    public override void Awake()
    {
        base.Awake();
        InitializeSaveable();
    }

    public void InitializeSaveable()
    {
        SaveManager.Instance.RegisterSaveable(this);
    }

    public string GetSaveString()
    {
        return new UserStationsData(stations.Values.Select(station => station.ToDataType()).ToList()).GetJson();
    }

    public void AddStations(IEnumerable<RadioStation> stations)
    {
        foreach (var station in stations)
        {
            if (station.Id == null)
                throw new ArgumentNullException(nameof(station.Id), "Station id cannot be null");

            uint idHash = station.Id.GetStableHashCode();

            if (this.stations.ContainsKey(idHash))
                throw new ArgumentException($"Radio station with ID '{station.Id}' already exists");

            this.stations.Add(idHash, station);
        }
    }
}
