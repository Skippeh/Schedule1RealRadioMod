namespace SongInfoFetcher.GlobalPlayer;

public static class ManagerExtensions
{
    public static void AddGlobalPlayerFetcher(this SongInfoFetchManager manager)
    {
        manager.AddFetcher(GlobalPlayerSongFetcher.UriRegex, uri => new GlobalPlayerSongFetcher(uri));
    }
}
