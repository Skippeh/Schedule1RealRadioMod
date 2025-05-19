using HarmonyLib;
using ScheduleOne.Audio;

namespace RealRadio.Patches;

[HarmonyPatch(typeof(AmbientTrack), nameof(AmbientTrack.Awake))]
internal static class AmbientTrackAwake
{
    public static void Prefix(AmbientTrack __instance)
    {
        // Disables ambient music from playing
        __instance.gameObject.SetActive(false);
    }
}
