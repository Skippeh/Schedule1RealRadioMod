using System;
using System.Linq;
using FishNet.Connection;
using RealRadio.Persistence.Data;
using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Property;
using UnityEngine;

namespace RealRadio.Components.Building;

public abstract class OffGridItem : BuildableItem
{
    public event Action? BeforeDestroy;

    public void InitializeOffGridItem(ItemInstance itemInstance, Vector3 position, Quaternion rotation, Guid guid)
    {
        if (Initialized)
            return;

        // todo: implement the below function manually to avoid property requirement
        InitializeBuildableItem(itemInstance, guid.ToString(), parentPropertyCode: Business.Businesses.First().PropertyCode);

        transform.position = position;
        transform.rotation = rotation;

        Initialized = true;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    /// <summary>
    /// Note: When overridden make sure to call this base method first
    /// </summary>
    protected virtual void OnDestroy()
    {
        BeforeDestroy?.Invoke();
    }

    public override BuildableItemData GetBaseData()
    {
        return new OffGridItemData(
            GUID,
            ItemInstance,
            loadOrder: 0,
            transform.position,
            transform.rotation.eulerAngles
        );
    }

    public override void SendInitializationToClient(NetworkConnection conn)
    {
    }

    public override void SendInitializationToServer()
    {
    }
}
