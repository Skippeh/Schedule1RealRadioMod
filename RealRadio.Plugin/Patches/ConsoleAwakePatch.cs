using System;
using HarmonyLib;

namespace RealRadio.Patches;

[HarmonyPatch(typeof(ScheduleOne.Console), nameof(ScheduleOne.Console.Awake))]
public static class ConsoleAwakePatch
{
    public static event Action<ScheduleOne.Console>? OnPostConsoleAwake;

    private static void Postfix(ScheduleOne.Console __instance)
    {
        OnPostConsoleAwake?.Invoke(__instance);
    }
}
