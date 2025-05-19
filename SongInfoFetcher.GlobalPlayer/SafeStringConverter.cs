using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SongInfoFetcher.GlobalPlayer;

internal class SafeStringEnumConverter<T> : StringEnumConverter
{
    static SafeStringEnumConverter()
    {
        if (!typeof(T).IsEnum)
            throw new ArgumentException("T must be an enum");
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(T);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        try
        {
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
        catch
        {
            return default;
        }
    }
}
