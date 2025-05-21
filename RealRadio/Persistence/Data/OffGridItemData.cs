using System;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence.Datas;
using UnityEngine;

namespace RealRadio.Persistence.Data;

[Serializable]
public class OffGridItemData : BuildableItemData
{
    public Vector3 Position;
    public Vector3 Rotation;

    public OffGridItemData(Guid guid, ItemInstance item, int loadOrder, Vector3 position, Vector3 rotation) : base(guid, item, loadOrder)
    {
        Position = position;
        Rotation = rotation;
        DataType = $"RealRadio_{GetType().Name}";
    }
}
