using System;
using BepInEx;
using BepInEx.Configuration;

namespace RealRadio.Plugin.BepInEx;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class BIEPlugin : BaseUnityPlugin
{
    private RealRadioPlugin? plugin;

    void Awake()
    {
        RealRadio.Logger.OnLog += OnLog;
        InitConfig();

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

    private void InitConfig()
    {
        RealRadio.Config.Instance = new BIEConfig(Config);
    }

    private class BIEConfig : IConfig
    {
        public event Action<string, IConfigData>? ValueChanged;

        public IConfigData Data { get; internal set; }

        internal readonly ConfigFile File;

        public BIEConfig(ConfigFile config)
        {
            File = config;
            Data = new BIEConfigData(this);
        }

        internal void OnValueChanged(string propertyName)
        {
            ValueChanged?.Invoke(propertyName, Data);
        }
    }

    private class BIEConfigData : IConfigData
    {
        public float MaxAudioHostInactivityTime
        {
            get => maxAudioHostInactivityTimeEntry.Value;
            set => maxAudioHostInactivityTimeEntry.Value = value;
        }

        public uint MaxInactiveAudioHosts
        {
            get => maxInaudibleAudioClientsEntry.Value;
            set => maxInaudibleAudioClientsEntry.Value = value;
        }

        private readonly ConfigEntry<float> maxAudioHostInactivityTimeEntry;
        private readonly ConfigEntry<uint> maxInaudibleAudioClientsEntry;

        public BIEConfigData(BIEConfig config)
        {
            maxAudioHostInactivityTimeEntry = config.File.Bind<float>(
                "General",
                nameof(MaxAudioHostInactivityTime),
                defaultValue: 30,
                new ConfigDescription("The maximum amount of time (in seconds) that an audio host can be inactive (no audible sound sources) before being stopped.", new AcceptableValueRange<float>(0, float.MaxValue))
            );
            maxInaudibleAudioClientsEntry = config.File.Bind<uint>(
                "General",
                nameof(MaxInactiveAudioHosts),
                defaultValue: 5,
                new ConfigDescription("The maximum number of audio hosts that can be inactive (not audible) before stopping the least recently played host.", new AcceptableValueRange<uint>(0, uint.MaxValue))
            );

            maxAudioHostInactivityTimeEntry.SettingChanged += (_, _) => config.OnValueChanged(nameof(MaxAudioHostInactivityTime));
            maxInaudibleAudioClientsEntry.SettingChanged += (_, _) => config.OnValueChanged(nameof(MaxInactiveAudioHosts));
        }
    }
}
