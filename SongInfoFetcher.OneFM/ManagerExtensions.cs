using System.Text.RegularExpressions;

namespace SongInfoFetcher.OneFM;

public static class ManagerExtensions
{
    public static void AddOneFMFetcher(this SongInfoFetchManager manager)
    {
        manager.AddFetcher(OneFMSongInfoFetcher.UriRegex, uri => new OneFMSongInfoFetcher(uri));
    }
}
