using System;
using HarmonyLib;
using ScheduleOne.Persistence;

[HarmonyPatch(typeof(LoadManager))]
internal static class LoadManagerPatches
{
    public static Action<LoadManager>? InitializeObjectLoaders;
    public static Action<LoadManager>? InitializeItemLoaders;

    [HarmonyPatch(nameof(LoadManager.InitializeObjectLoaders))]
    [HarmonyPostfix]
    private static void InitializeObjectLoadersPostfix(LoadManager __instance)
    {
        InitializeObjectLoaders?.Invoke(__instance);
    }

    [HarmonyPatch(nameof(LoadManager.InitializeItemLoaders))]
    [HarmonyPostfix]
    private static void InitializeItemLoadersPostfix(LoadManager __instance)
    {
        InitializeItemLoaders?.Invoke(__instance);
    }
}
