using System;
using SongInfoFetcher.OneFM;

namespace SongInfoFetcher.CliTest;

public static class Program
{
    public static void Main(string[] args)
    {
        Uri uri = new Uri(args[0]);

        Console.WriteLine($"Getting current song info for {uri}...");

        var manager = new SongInfoFetchManager();
        manager.AddOneFMFetcher();

        if (!manager.TryGetFetcher(uri, out var fetcher))
        {
            Console.WriteLine("No fetcher found");
            return;
        }
    }
}
