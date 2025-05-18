namespace SongInfoFetcher.TruckersFM;

public static class ManagerExtensions
{
    public static void AddTruckersFMSongInfoFetcher(this SongInfoFetchManager manager)
    {
        manager.AddFetcher(TruckersFMSongFetcher.UriRegex, uri => new TruckersFMSongFetcher());
    }
}
