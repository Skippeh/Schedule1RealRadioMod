// Uncomment the line below while editing this file, otherwise autocomplete etc will not work
//#define BEPINEX

#if BEPINEX

using System;
using BepInEx;
using LocalMultiplayer;
using UnityEngine;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class BIEPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        LocalMultiplayer.Logger.OnLog += OnLog;

        var go = new GameObject("LocalMultiplayer");
        go.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Plugin>();
    }

    private void OnLog(object data, LocalMultiplayer.Logger.LogLevel level)
    {
        switch (level)
        {
            case LocalMultiplayer.Logger.LogLevel.Debug:
                Logger.LogDebug(data);
                break;
            case LocalMultiplayer.Logger.LogLevel.Info:
                Logger.LogInfo(data);
                break;
            case LocalMultiplayer.Logger.LogLevel.Warning:
                Logger.LogWarning(data);
                break;
            case LocalMultiplayer.Logger.LogLevel.Error:
                Logger.LogError(data);
                break;
        }
    }
}

#endif
