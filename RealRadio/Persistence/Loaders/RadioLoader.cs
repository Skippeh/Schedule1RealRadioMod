using HashUtility;
using RealRadio.Components.Building.Buildables;
using RealRadio.Components.Radio;
using RealRadio.Persistence.Data;
using ScheduleOne.Persistence.Datas;

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

        if (Data.StationIdHash == 0 || !RadioStationManager.Instance.StationsByHashedId.TryGetValue(Data.StationIdHash, out var station))
        {
            if (Data.StationIdHash != 0)
                Plugin.Logger.LogWarning($"Could not find radio station with id {Data.StationIdHash}");
        }
        else
        {
            Item.SetRadioStationIdHash(Data.StationIdHash);
        }

        Item.SetVolume(Data.Volume);
    }
}
