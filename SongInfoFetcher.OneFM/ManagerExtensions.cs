using System.Text.RegularExpressions;

namespace SongInfoFetcher.OneFM;

public static class ManagerExtensions
{
    private static readonly Regex uriRegex = new Regex("^http(s)?://(.*?).1.fm/(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static void AddOneFMFetcher(this SongInfoFetchManager manager)
    {
        manager.AddFetcher(uriRegex, uri => new OneFMSongInfoFetcher(uri));
    }
}
