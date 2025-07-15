namespace RealRadio.Patches;

using System;
using FishNet;
using HarmonyLib;
using RealRadio.Assets;
using ScheduleOne;

[HarmonyPatch(typeof(Registry), nameof(Registry.Awake))]
public static class RegistryAwakePatch
{
    public static event Action<Registry>? RegistryAwake;

    public static void Prefix(Registry __instance)
    {
        RegistryAwake?.Invoke(__instance);
    }
}
