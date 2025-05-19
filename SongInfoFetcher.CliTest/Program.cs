using System;
using System.Collections.Generic;
using System.Linq;
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
        Uri[] uris = args.Select(arg => new Uri(arg)).ToArray();

        var manager = new SongInfoFetchManager();
        manager.AddOneFMFetcher();
        manager.AddSimulatorRadioFetcher();
        manager.AddTruckersFMSongInfoFetcher();
        manager.AddGlobalPlayerFetcher();

        List<Task> tasks = new(capacity: uris.Length);

        foreach (Uri uri in uris)
        {
            tasks.Add(Task.Run(async () =>
            {
                Console.WriteLine($"Getting current song info for {uri}...");

                ISongInfoFetcher? fetcher = await manager.GetFetcher(uri);

                if (fetcher == null)
                {
                    Console.WriteLine("No fetcher found");
                    return;
                }

                Console.WriteLine($"Got fetcher: {fetcher}");

                fetcher.SubscribeToSongInfoChanges(songInfo => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Song info changed on {uri}: {songInfo}"));

                var songInfo = await fetcher.RequestSongInfo();
                Console.WriteLine($"Got song info on {uri}: {songInfo}");
            }));
        }

        while (true)
        {
            await Task.Delay(1000);
        }
    }
}
