using System;
using RealRadio.Components.Building.Buildables;
using RealRadio.Persistence.Data;
using ScheduleOne.ItemFramework;
using UnityEngine;

namespace RealRadio.Persistence.Loaders;

[Serializable]
public class SpeakerData(Guid guid, ItemInstance item, int loadOrder, Vector3 position, Vector3 rotation, Guid? masterGuid, Speaker.AudioChannel channel, bool stereoOutput, Vector2 mountRotation) : OffGridItemData(guid, item, loadOrder, position, rotation)
{
    public string? MasterGuid = masterGuid?.ToString();
    public Speaker.AudioChannel Channel = channel;
    public bool StereoOutput = stereoOutput;
    public Vector2 MountRotation = mountRotation;
}
