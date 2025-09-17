using System;

namespace RealRadio;

public static class Config
{
    public static IConfig Instance
    {
        get => instance ?? throw new InvalidOperationException("Config has not been set");
        internal set => instance = value ?? throw new ArgumentNullException(nameof(value));
    }

    private static IConfig? instance;
}

public interface IConfigData
{
    public float MaxAudioHostInactivityTime { get; set; }
    public uint MaxInactiveAudioHosts { get; set; }
    public float BuildingMusicChance { get; set; }
    public float VehicleMusicChance { get; set; }
}

public interface IConfig
{
    event Action<string, IConfigData>? ValueChanged;

    IConfigData Data { get; }
}
