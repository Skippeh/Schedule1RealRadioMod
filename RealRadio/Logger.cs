using System;

namespace RealRadio;

internal static class Logger
{
    public static event Action<object, LogLevel>? OnLog;

    public static void LogInfo(object data)
    {
        OnLog?.Invoke(data, LogLevel.Info);
    }

    public static void LogWarning(object data)
    {
        OnLog?.Invoke(data, LogLevel.Warning);
    }

    public static void LogError(object data)
    {
        OnLog?.Invoke(data, LogLevel.Error);
    }

    public static void LogDebug(object data)
    {
        OnLog?.Invoke(data, LogLevel.Debug);
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
    }
}
