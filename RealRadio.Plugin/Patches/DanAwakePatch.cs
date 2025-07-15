using System;
using HarmonyLib;
using ScheduleOne.NPCs.CharacterClasses;

namespace RealRadio.Patches;

[HarmonyPatch(typeof(Dan), nameof(Dan.Awake))]
public static class DanAwakePatch
{
    public static event Action<Dan>? OnDanAwake;

    private static void Prefix(Dan __instance)
    {
        OnDanAwake?.Invoke(__instance);
    }
}
