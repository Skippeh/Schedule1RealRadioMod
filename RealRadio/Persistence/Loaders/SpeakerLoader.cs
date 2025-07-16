using System;
using RealRadio.Components.Building.Buildables;
using ScheduleOne.Persistence.Datas;

namespace RealRadio.Persistence.Loaders;

public class SpeakerLoader<TSpeaker, TSpeakerData> : OffGridItemLoader<TSpeaker, TSpeakerData>
    where TSpeaker : Speaker
    where TSpeakerData : SpeakerData
{
    public override void Load(DynamicSaveData data)
    {
        base.Load(data);

        if (Item == null || Data == null)
            return;

        Item.SelectedAudioChannel = Data.Channel;
        Item.StereoOutput = Data.StereoOutput;
        Item.MountRotation = Data.MountRotation;

        if (!string.IsNullOrEmpty(Data.MasterGuid))
        {
            var guid = Guid.TryParse(Data.MasterGuid, out Guid masterGuid) ? masterGuid : throw new ArgumentException($"Invalid radio guid in save data: {Data.MasterGuid}");

            var master = GUIDManager.GetObject<Radio>(guid);

            if (master == null)
                Logger.LogWarning($"Could not find radio while loading speaker. Radio guid: {Data.MasterGuid}");

            Item.MasterGuid = masterGuid;
        }
    }
}
