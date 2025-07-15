using System;
using FishNet.Object;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using UnityEngine;

namespace RealRadio.Components.Building;

public class OffGridBuildManager : NetworkSingleton<OffGridBuildManager>
{
    public override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        gameObject.hideFlags = HideFlags.HideAndDontSave;
    }

    /// <summary>
    /// Spawns a building. If this is the server, it will spawn the building instantly and return the building.
    /// Otherwise it will request the server to spawn the building and return null.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if itemInstance.Definition is not BuildableItemDefinition.</exception>
    public OffGridItem? SpawnBuilding(ItemInstance itemInstance, Vector3 position, Quaternion rotation, Guid? guid = null)
    {
        if (itemInstance.Definition is not BuildableItemDefinition itemDefinition)
        {
            throw new ArgumentException("itemInstance.Definition is not BuildableItemDefinition");
        }

        if (NetworkManager.IsServer)
        {
            guid ??= GUIDManager.GenerateUniqueGUID();
            OffGridItem item = Instantiate(itemDefinition.BuiltItem.gameObject).GetComponent<OffGridItem>();
            item.SetLocallyBuilt();
            item.InitializeOffGridItem(itemInstance, position, rotation, guid.Value);
            NetworkObject.Spawn(item.gameObject);
            return item;
        }
        else
        {
            RequestSpawnBuilding(itemInstance, position, rotation);
            return null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnBuilding(ItemInstance itemInstance, Vector3 position, Quaternion rotation)
    {
        if (itemInstance == null)
            return;

        SpawnBuilding(itemInstance, position, rotation);
    }
}
