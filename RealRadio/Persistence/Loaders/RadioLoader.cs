using System;
using System.Collections;
using HashUtility;
using RealRadio.Components.Building.Buildables;
using RealRadio.Components.Radio;
using RealRadio.Persistence.Data;
using ScheduleOne.Persistence.Datas;
using UnityEngine;

namespace RealRadio.Persistence.Loaders;

public class RadioLoader<TRadio, TRadioData> : TogglableOffGridItemLoader<TRadio, TRadioData>
    where TRadio : Radio
    where TRadioData : RadioData
{
    public override void Load(DynamicSaveData data)
    {
        base.Load(data);

        if (Item == null || Data == null)
            return;

        Item.SetVolume(Data.Volume);

        Item.StartCoroutine(WaitForUserStationsToLoad());

        IEnumerator WaitForUserStationsToLoad()
        {
            yield return new WaitUntil(() => UserStationsManager.Instance.SaveDataLoaded);

            if (Data.StationIdHash == 0 || !RadioStationManager.Instance.StationsByHashedId.TryGetValue(Data.StationIdHash, out var station))
            {
                if (Data.StationIdHash != 0)
                    Plugin.Logger.LogWarning($"Could not find radio station with id {Data.StationIdHash}");
            }
            else
            {
                Item.SetRadioStationIdHash(Data.StationIdHash);
            }

            for (byte i = 0; i < Math.Min(Item.MaxFavoriteStations, Data.FavoriteStations.Length); ++i)
            {
                uint hashId = Data.FavoriteStations[i];

                if (hashId == 0)
                    continue;

                if (!RadioStationManager.Instance.StationsByHashedId.TryGetValue(hashId, out var favStation))
                {
                    Plugin.Logger.LogWarning($"Could not find favorite radio station with id {hashId}");
                }
                else
                {
                    Item.SetFavoriteStation(i, favStation);
                }
            }
        }
    }
}
