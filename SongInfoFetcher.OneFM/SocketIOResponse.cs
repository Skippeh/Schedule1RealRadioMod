using Newtonsoft.Json;

namespace SongInfoFetcher.OneFM;

internal record SocketIOResponse
{
    [JsonProperty("sid")]
    public string? SessionId { get; set; }

    [JsonProperty("upgrades")]
    public string[]? Upgrades { get; set; }

    [JsonProperty("pingInterval")]
    public uint PingInterval { get; set; }

    [JsonProperty("pingTimeout")]
    public uint PingTimeout { get; set; }
}
