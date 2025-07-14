using RealRadio.Components.Building.Buildables;

namespace RealRadio.Persistence.Loaders;

public class SpeakerLoader<TSpeaker, TSpeakerData> : OffGridItemLoader<TSpeaker, TSpeakerData>
    where TSpeaker : Speaker
    where TSpeakerData : SpeakerData
{
}
