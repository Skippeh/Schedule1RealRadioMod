using System;
using System.Diagnostics.CodeAnalysis;
using RealRadio.Components.Building;
using RealRadio.Persistence.Data;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence;
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

    public override void Load(string mainPath)
    {
        TryLoadAndCreate(mainPath, out var item);
    }

    [MemberNotNullWhen(true, nameof(Data), nameof(Item))]
    protected virtual bool TryLoadAndCreate(string mainPath, out TItem? item)
    {
        Item = null;
        item = null;
        Data = null;

        if (!TryLoadFile(mainPath, "Data", out string contents))
            return false;

        try
        {
            Data = JsonUtility.FromJson<TLoadData>(contents);

            if (Data == null)
            {
                Plugin.Logger.LogError($"{GetType().Name} failed to deserialize data from '{mainPath}'");
                return false;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"{GetType().Name} failed to read data from '{mainPath}': {ex}");
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
            Plugin.Logger.LogError($"{GetType()} failed to parse guid from {mainPath}: '{Data.GUID}'");
            return false;
        }

        Item = item = OffGridBuildManager.Instance.SpawnBuilding(itemInstance, Data.Position, Quaternion.Euler(Data.Rotation), guid) as TItem;
        return Item != null;
    }
}
