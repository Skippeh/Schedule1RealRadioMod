using MelonLoader;
using RealRadio;

[assembly: MelonInfo(typeof(RealRadio.Plugin.ML.MLMod), MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, author: "Skipcast")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: HarmonyDontPatchAll]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]

namespace RealRadio.Plugin.ML;

public class MLMod : MelonMod
{
    private RealRadioPlugin? plugin;

    public override void OnInitializeMelon()
    {
        Logger.OnLog += OnLog;

        plugin = new RealRadioPlugin();
    }

    private static void OnLog(object data, Logger.LogLevel level)
    {
        var log = Melon<MLMod>.Logger;

        switch (level)
        {
            case Logger.LogLevel.Debug:
                if (LoaderConfig.Current.Loader.DebugMode)
                    log.Msg(System.ConsoleColor.Cyan, data);
                break;
            case Logger.LogLevel.Info:
                log.Msg(System.ConsoleColor.White, data);
                break;
            case Logger.LogLevel.Warning:
                log.Warning(data);
                break;
            case Logger.LogLevel.Error:
                log.Error(data);
                break;
        }
    }
}
