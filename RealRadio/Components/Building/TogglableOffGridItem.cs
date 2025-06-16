using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using RealRadio.Persistence.Data;
using ScheduleOne.Building;
using ScheduleOne.Interaction;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace RealRadio.Components.Building;

public abstract class TogglableOffGridItem : OffGridItem
{
    public event Action<bool>? Toggled;

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.ExcludeOwner, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnStateToggled))]
    public bool IsOn { get; [ServerRpc(RequireOwnership = false, RunLocally = true)] set; }

    protected virtual void OnStateToggled(bool prev, bool next, bool asServer)
    {
        if (!asServer)
            Toggled?.Invoke(next);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    public override BuildableItemData GetBaseData()
    {
        return new TogglableOffGridItemData(
            GUID,
            ItemInstance,
            loadOrder: 0,
            IsOn,
            transform.position,
            transform.rotation.eulerAngles
        );
    }
}
