using System;
using ScheduleOne.ItemFramework;
using UnityEngine;

namespace RealRadio.Persistence.Data;

[Serializable]
public class TogglableOffGridItemData(Guid guid, ItemInstance item, int loadOrder, bool isOn, Vector3 position, Vector3 rotation)
    : OffGridItemData(guid, item, loadOrder, position, rotation)
{
    public bool IsOn = isOn;
}
