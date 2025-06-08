using RealRadio.Components.Building;
using RealRadio.Persistence.Data;
using ScheduleOne.Persistence.Datas;

namespace RealRadio.Persistence.Loaders;

public class TogglableOffGridItemLoader<TItem, TLoadData> : OffGridItemLoader<TItem, TLoadData>
    where TItem : TogglableOffGridItem
    where TLoadData : TogglableOffGridItemData
{
    public override void Load(DynamicSaveData data)
    {
        if (!TryLoadAndCreate(data, out var _))
            return;

        Item.IsOn = Data.IsOn;
    }
}
