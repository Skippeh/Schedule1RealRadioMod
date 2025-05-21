using System;
using ScheduleOne.ItemFramework;
using UnityEngine;

namespace RealRadio.Persistence.Data;

[Serializable]
public class RadioData(Guid guid, ItemInstance item, int loadOrder, bool isOn, Vector3 position, Vector3 rotation, uint? stationIdHash, float volume)
    : TogglableOffGridItemData(guid, item, loadOrder, isOn, position, rotation)
{
    public uint StationIdHash = stationIdHash ?? 0;
    public float Volume = volume;
}
