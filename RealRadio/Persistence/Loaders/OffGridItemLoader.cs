using System;
using System.Diagnostics.CodeAnalysis;
using RealRadio.Components.Building;
using RealRadio.Persistence.Data;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;
using UnityEngine;

namespace RealRadio.Persistence.Loaders;

public class OffGridItemLoader<TItem, TLoadData> : BuildableItemLoader
    where TItem : OffGridItem
    where TLoadData : OffGridItemData
{
    public override string ItemType => $"RealRadio_{typeof(TLoadData).Name}";

    /// <summary>
    /// The last loaded data from when <see cref="TryLoadAndCreate"/> was last called.
    /// </summary>
    protected TLoadData? Data { get; private set; }

    /// <summary>
    /// The last loaded item from when <see cref="TryLoadAndCreate"/> was last called.
    /// </summary>
    protected TItem? Item { get; private set; }

    public override void Load(DynamicSaveData data)
    {
        TryLoadAndCreate(data, out var item);
    }

    [MemberNotNullWhen(true, nameof(Data), nameof(Item))]
    protected virtual bool TryLoadAndCreate(DynamicSaveData data, out TItem? item)
    {
        Item = null;
        item = null;
        Data = null;

        try
        {
            Data = JsonUtility.FromJson<TLoadData>(data.BaseData);

            if (Data == null)
            {
                Plugin.Logger.LogError($"{GetType().Name} failed to deserialize data of type '{data.DataType}'");
                return false;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"{GetType().Name} failed to read data of type '{data.DataType}': {ex}");
            return false;
        }

        ItemInstance itemInstance = ItemDeserializer.LoadItem(Data.ItemString);

        if (itemInstance.Definition is not BuildableItemDefinition buildableItemDefinition)
        {
            Plugin.Logger.LogError($"{GetType()} failed to get buildable item definition from '{Data.ItemString}'");
            return false;
        }

        if (buildableItemDefinition.BuiltItem is not TItem)
        {
            Plugin.Logger.LogError($"{GetType()} failed to get correct item type from '{Data.ItemString}' (expected {typeof(TItem).Name} but got {buildableItemDefinition.BuiltItem.GetType().Name})");
            return false;
        }

        if (itemInstance == null)
        {
            Plugin.Logger.LogError($"{GetType()} failed to get item instance from '{Data.ItemString}'");
            return false;
        }

        if (!Guid.TryParse(Data.GUID, out var guid))
        {
            Plugin.Logger.LogError($"{GetType()} failed to parse guid: '{Data.GUID}'");
            return false;
        }

        Item = item = OffGridBuildManager.Instance.SpawnBuilding(itemInstance, Data.Position, Quaternion.Euler(Data.Rotation), guid) as TItem;
        return Item != null;
    }
}
