using System;
using System.IO;
using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.Utils;
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
        InitConfig();

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

    private void InitConfig()
    {
        var category = MelonPreferences.CreateCategory(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME);
        category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "RealRadio.cfg"), autoload: false);
        Config.Instance = new MLConfig(category);
        category.LoadFromFile();
    }

    class MLConfig : IConfig
    {
        public IConfigData Data { get; internal set; }

        public event Action<string, IConfigData>? ValueChanged;

        internal readonly MelonPreferences_Category Category;

        public MLConfig(MelonPreferences_Category category)
        {
            Category = category;
            Data = new MLConfigData(this);
        }

        internal void OnValueChanged(string propertyName)
        {
            ValueChanged?.Invoke(propertyName, Data);
        }
    }

    private class MLConfigData : IConfigData
    {
        private readonly MelonPreferences_Entry<float> maxAudioHostInactivityTimeEntry;
        private readonly MelonPreferences_Entry<uint> maxInaudibleAudioClientsEntry;
        private readonly MelonPreferences_Entry<float> buildingMusicChanceEntry;
        private readonly MelonPreferences_Entry<float> vehicleMusicChanceEntry;
        private readonly MelonPreferences_Entry<bool> enableBuildingMusicEntry;
        private readonly MelonPreferences_Entry<bool> enableVehicleMusicEntry;

        public MLConfigData(MLConfig config)
        {
            maxAudioHostInactivityTimeEntry = config.Category.CreateEntry<float>(
                nameof(MaxAudioHostInactivityTime),
                default_value: 30,
                display_name: "Max audio host inactivity time",
                description: "The maximum amount of time (in seconds) that an audio host can be inactive (no audible sound sources) before being stopped.",
                validator: new ValueRange<float>(0, uint.MaxValue)
            );
            maxInaudibleAudioClientsEntry = config.Category.CreateEntry<uint>(
                nameof(MaxInactiveAudioHosts),
                default_value: 5,
                display_name: "Max inaudible audio hosts",
                description: "The maximum number of audio hosts that can be inactive (not audible) before stopping the least recently played host.",
                validator: new ValueRange<uint>(0, uint.MaxValue)
            );
            buildingMusicChanceEntry = config.Category.CreateEntry(
                nameof(BuildingMusicChance),
                default_value: 0.5f,
                display_name: "Building music chance",
                description: "The chance that a radio station will be played from residential buildings when an NPC enters it. Note: in multiplayer, the value of the hosting player will be used.",
                validator: new ValueRange<float>(0, 1)
            );
            vehicleMusicChanceEntry = config.Category.CreateEntry(
                nameof(VehicleMusicChance),
                default_value: 0.5f,
                display_name: "Vehicle music chance",
                description: "The chance that a radio station will be played from a vehicle when an NPC drives it. Note: in multiplayer, the value of the hosting player will be used.",
                validator: new ValueRange<float>(0, 1)
            );
            enableBuildingMusicEntry = config.Category.CreateEntry(
                nameof(EnableBuildingMusic),
                default_value: true,
                display_name: "Play random radio stations from residential buildings",
                description: "If enabled, a random radio station may be played from residential buildings when an NPC enters it. In multiplayer this setting only affects yourself and also works as a non-hosting player."
            );
            enableVehicleMusicEntry = config.Category.CreateEntry(
                nameof(EnableVehicleMusic),
                default_value: true,
                display_name: "Play random radio stations from NPC vehicles",
                description: "If enabled, a random radio station may be played from NPC vehicles when an NPC drives it. In multiplayer this setting only affects yourself and also works as a non-hosting player."
            );

            maxAudioHostInactivityTimeEntry.OnEntryValueChanged.Subscribe((_, _) => config.OnValueChanged(nameof(MaxAudioHostInactivityTime)));
            maxInaudibleAudioClientsEntry.OnEntryValueChanged.Subscribe((_, _) => config.OnValueChanged(nameof(MaxInactiveAudioHosts)));
            buildingMusicChanceEntry.OnEntryValueChanged.Subscribe((_, _) => config.OnValueChanged(nameof(BuildingMusicChance)));
            vehicleMusicChanceEntry.OnEntryValueChanged.Subscribe((_, _) => config.OnValueChanged(nameof(VehicleMusicChance)));
            enableBuildingMusicEntry.OnEntryValueChanged.Subscribe((_, _) => config.OnValueChanged(nameof(EnableBuildingMusic)));
            enableVehicleMusicEntry.OnEntryValueChanged.Subscribe((_, _) => config.OnValueChanged(nameof(EnableVehicleMusic)));
        }

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

        public float BuildingMusicChance
        {
            get => buildingMusicChanceEntry.Value;
            set => buildingMusicChanceEntry.Value = value;
        }
        public float VehicleMusicChance
        {
            get => vehicleMusicChanceEntry.Value;
            set => vehicleMusicChanceEntry.Value = value;
        }

        public bool EnableBuildingMusic
        {
            get => enableBuildingMusicEntry.Value;
            set => enableBuildingMusicEntry.Value = value;
        }
        public bool EnableVehicleMusic
        {
            get => enableVehicleMusicEntry.Value;
            set => enableVehicleMusicEntry.Value = value;
        }
    }
}
