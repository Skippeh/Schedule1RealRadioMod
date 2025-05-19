namespace SongInfoFetcher.TruckersFM.Data;

internal record HttpCurrentSongResponse
{
    public string? Status { get; set; }
    public SongData? Data { get; set; }
}
