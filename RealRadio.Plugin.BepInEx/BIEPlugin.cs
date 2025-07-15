using System;
using BepInEx;

namespace RealRadio.Plugin.BepInEx;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class BIEPlugin : BaseUnityPlugin
{
    private RealRadioPlugin? plugin;

    void Awake()
    {
        RealRadio.Logger.OnLog += OnLog;

        plugin = new RealRadioPlugin();
    }

    private void OnLog(object data, Logger.LogLevel level)
    {
        switch (level)
        {
            case RealRadio.Logger.LogLevel.Debug:
                Logger.LogDebug(data);
                break;
            case RealRadio.Logger.LogLevel.Info:
                Logger.LogInfo(data);
                break;
            case RealRadio.Logger.LogLevel.Warning:
                Logger.LogWarning(data);
                break;
            case RealRadio.Logger.LogLevel.Error:
                Logger.LogError(data);
                break;
        }
    }
}
