using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RealRadio.Components.API.Data;
using RealRadio.Components.Radio;
using RealRadio.Peristence.Data;
using ScheduleOne.Persistence.Loaders;
using UnityEngine;

namespace RealRadio.Persistence.Loaders;

public class UserStationsLoader : Loader
{
    public override void Load(string mainPath)
    {
        if (!TryLoadFile(mainPath, out string contents))
            return;

        UserStationsData data;

        try
        {
            data = JsonConvert.DeserializeObject<UserStationsData>(contents) ?? throw new JsonSerializationException("Deserialized object is null");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"Failed to read user radio stations from '{mainPath}': {ex}");
            return;
        }

        if (data.Stations == null)
        {
            Plugin.Logger.LogError($"Loaded user radio stations from '{mainPath}' are null");
            return;
        }

        List<RadioStation> validatedStations = new(capacity: data.Stations.Count);

        foreach (var station in data.Stations)
        {
            if (!station.IsValid(out var invalidReasons))
            {
                Plugin.Logger.LogError($"Could not validate user radio station '{station.Id} ({station.Name})':\n- {string.Join("\n- ", invalidReasons)}");
                continue;
            }

            validatedStations.Add(station);
        }

        try
        {
            UserStationsManager.Instance.AddOrUpdateStations(validatedStations);

            if (validatedStations.Count != data.Stations.Count)
                Plugin.Logger.LogWarning($"Loaded {validatedStations.Count} out of {data.Stations.Count} user radio station(s)");
            else
                Plugin.Logger.LogInfo($"Successfully loaded {data.Stations.Count} user radio station(s)");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"Failed to register user radio stations: {ex}");
        }
    }
}
