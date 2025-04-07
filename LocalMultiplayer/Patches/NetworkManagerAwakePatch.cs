using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using HarmonyLib;

namespace LocalMultiplayer.Patches;

[HarmonyPatch(typeof(Multipass), nameof(Multipass.Initialize))]
public static class NetworkManagerAwakePatch
{
    public static void Prefix(Multipass __instance)
    {
        var tugboat = __instance.gameObject.AddComponent<Tugboat>();
        tugboat.SetServerBindAddress("127.0.0.1", IPAddressType.IPv4);
        tugboat.SetMaximumClients(4);
        tugboat.SetClientAddress("127.0.0.1");
        tugboat.SetPort(7777);

        __instance._transports.Add(tugboat);
    }
}
