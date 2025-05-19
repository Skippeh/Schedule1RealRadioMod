using Newtonsoft.Json;

namespace SongInfoFetcher.OneFM.Data;

public record NewsData
{
    [JsonProperty("stid")]
    public string? StationId { get; set; }

    [JsonProperty("name")]
    public string? StationName { get; set; }
    public SongData[]? NowPlaying { get; set; } = [];
}

public record SongData
{
    public string? Artist { get; set; }
    public string? Title { get; set; }
}
