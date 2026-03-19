using System;
using HarmonyLib;
using ScheduleOne.UI.Phone;

namespace RealRadio.Patches;

[HarmonyPatch(typeof(AppsCanvas))]
internal static class AppsCanvasPatches
{
    public static Action<AppsCanvas>? CanvasCreated;

    [HarmonyPatch(nameof(AppsCanvas.Awake))]
    [HarmonyPostfix]
    private static void AppsCanvasAwakePostFix(AppsCanvas __instance)
    {
        CanvasCreated?.Invoke(__instance);
    }
}
