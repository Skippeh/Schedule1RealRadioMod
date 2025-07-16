using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RealRadio.Components.API.Data;
using RealRadio.Components.Radio;
using RealRadio.Peristence.Data;
using ScheduleOne.Persistence.Loaders;

namespace RealRadio.Persistence.Loaders;

public class UserStationsLoader : Loader
{
    public override void Load(string mainPath)
    {
        try
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
                Logger.LogError($"Failed to read user radio stations from '{mainPath}': {ex}");
                return;
            }

            if (data.Stations == null)
            {
                Logger.LogError($"Loaded user radio stations from '{mainPath}' are null");
                return;
            }

            List<RadioStation> validatedStations = new(capacity: data.Stations.Count);

            foreach (var station in data.Stations)
            {
                if (!station.IsValid(out var invalidReasons))
                {
                    Logger.LogError($"Could not validate user radio station '{station.Id} ({station.Name})':\n- {string.Join("\n- ", invalidReasons)}");
                    continue;
                }

                validatedStations.Add(station);
            }

            try
            {
                UserStationsManager.Instance.AddOrUpdateStations(validatedStations);

                if (validatedStations.Count != data.Stations.Count)
                    Logger.LogWarning($"Loaded {validatedStations.Count} out of {data.Stations.Count} user radio station(s)");
                else
                    Logger.LogInfo($"Successfully loaded {data.Stations.Count} user radio station(s)");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to register user radio stations: {ex}");
            }
        }
        finally
        {
            UserStationsManager.Instance.OnStationsLoaded?.Invoke();
        }
    }
}
