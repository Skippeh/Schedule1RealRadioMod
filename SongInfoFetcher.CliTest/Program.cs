using System;
using System.Threading.Tasks;
using SongInfoFetcher.OneFM;

namespace SongInfoFetcher.CliTest;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Uri uri = new Uri(args[0]);

        Console.WriteLine($"Getting current song info for {uri}...");

        var manager = new SongInfoFetchManager();
        manager.AddOneFMFetcher();

        //if (!manager.TryGetFetcher(uri, out var fetcher))
        ISongInfoFetcher? fetcher;

        if ((fetcher = await manager.TryGetFetcher(uri)) == null)
        {
            Console.WriteLine("No fetcher found");
            return;
        }

        Console.WriteLine($"Got fetcher: {fetcher}");
    }
}
