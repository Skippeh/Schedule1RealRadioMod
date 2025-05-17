using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace SongInfoFetcher;

public class SongInfoFetchManager
{
    public ReadOnlyDictionary<Regex, ISongInfoFetcher> Fetchers => new ReadOnlyDictionary<Regex, ISongInfoFetcher>(fetchers);

    private Dictionary<Regex, ISongInfoFetcher> fetchers = [];

    public void AddFetcher(Regex uriRegex, ISongInfoFetcher fetcher) =>
        fetchers.Add(
            uriRegex ?? throw new ArgumentNullException(nameof(uriRegex)),
            fetcher ?? throw new ArgumentNullException(nameof(fetcher))
        );

    /// <summary>
    /// Adds a fetcher that matches the specified uri's scheme and authority (host and port) with any path.
    /// </summary>
    public void AddFetcher(Uri uri, ISongInfoFetcher fetcher) => AddFetcher(CreateBasicRegex(uri), fetcher);

    /// <summary>
    /// Creates a basic regex that matches the specified uri's scheme and authority (host and port) with any path.
    /// </summary>
    public static Regex CreateBasicRegex(Uri uri)
    {
        return new Regex($@"^{Regex.Escape(uri.Scheme)}://{Regex.Escape(uri.Authority)}(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Returns the first fetcher that matches the specified url, if any.
    /// </summary>
    public bool TryGetFetcher(string url, out ISongInfoFetcher? outFetcher)
    {
        foreach (var fetcher in fetchers)
        {
            var match = fetcher.Key.Match(url);
            if (match.Success)
            {
                outFetcher = fetcher.Value;
                return true;
            }
        }

        outFetcher = null;
        return false;
    }
}
