using System;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using SongInfoFetcher.GlobalPlayer;
using SongInfoFetcher.OneFM;
using SongInfoFetcher.SimulatorRadio;
using SongInfoFetcher.TruckersFM;

namespace SongInfoFetcher.CliTest;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Uri uri = new Uri(args[0]);

        Console.WriteLine($"Getting current song info for {uri}...");

        var manager = new SongInfoFetchManager();
        manager.AddOneFMFetcher();
        manager.AddSimulatorRadioFetcher();
        manager.AddTruckersFMSongInfoFetcher();
        manager.AddGlobalPlayerFetcher();

        ISongInfoFetcher? fetcher = await manager.GetFetcher(uri);

        if (fetcher == null)
        {
            Console.WriteLine("No fetcher found");
            return;
        }

        Console.WriteLine($"Got fetcher: {fetcher}");

        fetcher.SubscribeToSongInfoChanges(songInfo => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Song info changed: {songInfo}"));

        var songInfo = await fetcher.RequestSongInfo();
        Console.WriteLine($"Got song info: {songInfo}");

        while (true)
        {
            await Task.Delay(1000);
        }
    }
}
