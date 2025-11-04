using System;

namespace SongInfoFetcher.OneFM.Data;

public record HistorySong
{
    public DateTime Timestamp { get; set; }
    public string? ChannelId { get; set; }
    public string? ChannelName { get; set; }
    public string? Artist { get; set; }
    public string? Title { get; set; }
}
