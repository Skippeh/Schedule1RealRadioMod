using System;
using HarmonyLib;
using ScheduleOne.Audio;

[HarmonyPatch(typeof(MusicTrack))]
internal static class MusicTrackPatches
{
    public static Action<MusicTrack, bool>? MusicTrackToggled { get; set; }
    public static Action<MusicTrack>? MusicTrackPlay { get; set; }

    [HarmonyPatch(nameof(MusicTrack.Enable))]
    [HarmonyPostfix]
    public static void MusicTrackEnabledPostFix(MusicTrack __instance)
    {
        MusicTrackToggled?.Invoke(__instance, true);
    }

    [HarmonyPatch(nameof(MusicTrack.Disable))]
    [HarmonyPostfix]
    public static void MusicTrackDisabledPostFix(MusicTrack __instance)
    {
        MusicTrackToggled?.Invoke(__instance, false);
    }

    [HarmonyPatch(nameof(MusicTrack.Play))]
    [HarmonyPostfix]
    public static void MusicTrackPlayPostFix(MusicTrack __instance)
    {
        MusicTrackPlay?.Invoke(__instance);
    }
}
