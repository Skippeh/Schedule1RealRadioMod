using RealRadio.Components.Building;
using RealRadio.Components.Building.Buildables;
using RealRadio.Persistence.Data;
using RealRadio.Persistence.Loaders;
using ScheduleOne.Persistence;

namespace RealRadio.Persistence;

internal static class Persistence
{
    public static void AddObjectInitializers(LoadManager loadManager)
    {
        // The constructor of each loader will register itself
        new OffGridItemLoader<OffGridItem, OffGridItemData>();
        new TogglableOffGridItemLoader<TogglableOffGridItem, TogglableOffGridItemData>();
        new RadioLoader<Radio, RadioData>();
        new SpeakerLoader<Speaker, SpeakerData>();
    }

    public static void AddItemInitializers(LoadManager loadManager)
    {
    }
}
