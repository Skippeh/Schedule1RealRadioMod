using Newtonsoft.Json;
using SongInfoFetcher.TruckersFM.Data;

namespace SongInfoFetcher.TruckersFM;

internal record SongEventData
{
    [JsonProperty("current_song")]
    public CurrentSongData? CurrentSong { get; set; }
}

internal record CurrentSongData
{
    public SongData? Song { get; set; }
}
