using System.Runtime.Serialization;
using Newtonsoft.Json;

internal record NowPlayingData
{
    public string? Artists { get; set; }
    public string? Title { get; set; }
}

internal record Message
{
    public MessageType Type { get; set; }

    [JsonProperty("now_playing")]
    public NowPlayingData? NowPlaying { get; set; }
}

internal enum MessageType
{
    NowPlaying,
    History,

    [EnumMember(Value = "24hours")]
    Last24Hours,

    // unused but existing types:
    // history
    // 24hours
}
