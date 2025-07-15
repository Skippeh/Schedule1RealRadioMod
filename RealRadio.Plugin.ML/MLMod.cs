using System;
using System.Collections;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(RealRadio.Plugin.ML.MLMod), "RealRadio", "1.0.0", "Skipcast")]
[assembly: MelonGame("TVGS", "Schedule I")]

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
