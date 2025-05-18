namespace SongInfoFetcher.SimulatorRadio;

public static class ManagerExtensions
{
    public static void AddSimulatorRadioFetcher(this SongInfoFetchManager manager)
    {
        manager.AddFetcher(SimulatorRadioSongInfoFetcher.UriRegex, uri => new SimulatorRadioSongInfoFetcher());
    }
}
