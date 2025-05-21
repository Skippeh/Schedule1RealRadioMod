using RealRadio.Components.Building;
using RealRadio.Persistence.Data;

namespace RealRadio.Persistence.Loaders;

public class TogglableOffGridItemLoader<TItem, TLoadData> : OffGridItemLoader<TItem, TLoadData>
    where TItem : TogglableOffGridItem
    where TLoadData : TogglableOffGridItemData
{
    public override void Load(string mainPath)
    {
        if (!TryLoadAndCreate(mainPath, out var _))
            return;

        Item.IsOn = Data.IsOn;
    }
}
