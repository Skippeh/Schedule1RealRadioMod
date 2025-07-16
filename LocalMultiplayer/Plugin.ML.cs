// Uncomment the line below while editing this file, otherwise autocomplete etc will not work
//#define MELONLOADER

#if MELONLOADER

using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(LocalMultiplayer.MLMod), "LocalMultiplayer", "1.0.0", "Skipcast")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: HarmonyDontPatchAll]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]

namespace LocalMultiplayer;

public class MLMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        Logger.OnLog += OnLog;

        var go = new GameObject("LocalMultiplayer");
        go.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Plugin>();
    }

    private void OnLog(object data, Logger.LogLevel level)
    {
        switch (level)
        {
            case Logger.LogLevel.Debug:
                if (LoaderConfig.Current.Loader.DebugMode)
                    LoggerInstance.Msg(System.ConsoleColor.Cyan, data);
                break;
            case Logger.LogLevel.Info:
                LoggerInstance.Msg(System.ConsoleColor.White, data);
                break;
            case Logger.LogLevel.Warning:
                LoggerInstance.Warning(data);
                break;
            case Logger.LogLevel.Error:
                LoggerInstance.Error(data);
                break;
        }
    }
}

#endif
