using System.Collections;
using System.IO;
using RealRadio.Components.Radio;
using ScheduleOne.DevUtilities;
using UnityEngine;

namespace RealRadio.Components.API;

public class APIManager : PersistentSingleton<APIManager>
{
    public CustomRadioStations RadioStations { get; private set; } = null!;

    public override void Awake()
    {
        base.Awake();

        string stationsRootDirectory = Path.Combine(Application.dataPath, "..", "RealRadio", "Stations");
        RadioStations = new(stationsRootDirectory);
    }

    public IEnumerator LoadDataCoroutine()
    {
        Plugin.Logger.LogInfo("Loading custom radio stations...");
        var loadStationsTask = RadioStations.LoadStationsFromDisk();

        yield return new WaitUntil(() => loadStationsTask.IsCompleted);

        if (loadStationsTask.IsFaulted)
        {
            Plugin.Logger.LogError($"Failed to load custom radio stations:\n{loadStationsTask.Exception}");
            yield break;
        }

        var stations = loadStationsTask.Result;

        foreach (var station in stations)
        {
            Plugin.Logger.LogInfo($"Registering custom radio station: {station.Name} ({station.Id})");
            RadioStationManager.Instance.AddRadioStation(station);
        }
    }
}
