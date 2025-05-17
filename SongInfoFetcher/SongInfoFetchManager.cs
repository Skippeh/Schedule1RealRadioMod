using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace SongInfoFetcher;

public class SongInfoFetchManager
{
    public ReadOnlyDictionary<Regex, Dictionary<Uri, ISongInfoFetcher>> Fetchers => new(fetchers);

    private Dictionary<Regex, Dictionary<Uri, ISongInfoFetcher>> fetchers = [];
    private Dictionary<Regex, Func<Uri, ISongInfoFetcher>> fetcherFactories = [];

    public void AddFetcher(Regex uriRegex, Func<Uri, ISongInfoFetcher> fetcherFactory)
    {
        if (uriRegex == null)
            throw new ArgumentNullException(nameof(uriRegex));

        if (fetcherFactory == null)
            throw new ArgumentNullException(nameof(fetcherFactory));

        fetchers.Add(uriRegex, []);
        fetcherFactories.Add(uriRegex, fetcherFactory);
    }

    /// <summary>
    /// Adds a fetcher that matches the specified uri's scheme and authority (host and port) with any path.
    /// </summary>
    public void AddFetcher(Uri uri, Func<Uri, ISongInfoFetcher> fetcherFactory) => AddFetcher(CreateBasicRegex(uri), fetcherFactory);

    /// <summary>
    /// Creates a basic regex that matches the specified uri's scheme and authority (host and port) with any path.
    /// </summary>
    public static Regex CreateBasicRegex(Uri uri)
    {
        return new Regex($@"^{Regex.Escape(uri.Scheme)}://{Regex.Escape(uri.Authority)}(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Returns the first fetcher that matches the specified uri, if any.
    /// </summary>
    public bool TryGetFetcher(Uri uri, out ISongInfoFetcher? outFetcher)
    {
        foreach (var fetcher in fetchers)
        {
            var match = fetcher.Key.Match(uri.ToString());

            if (match.Success)
            {
                if (fetcher.Value.TryGetValue(uri, out outFetcher))
                    return true;

                outFetcher = fetcherFactories[fetcher.Key](uri);
                fetcher.Value.Add(uri, outFetcher);
                return true;
            }
        }

        outFetcher = null;
        return false;
    }
}
