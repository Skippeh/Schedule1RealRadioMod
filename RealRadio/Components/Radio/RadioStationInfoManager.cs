using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using RealRadio.Components.API.Data;
using ScheduleOne.DevUtilities;
using SongInfoFetcher;
using SongInfoFetcher.GlobalPlayer;
using SongInfoFetcher.OneFM;
using SongInfoFetcher.SimulatorRadio;
using SongInfoFetcher.TruckersFM;
using UnityEngine;

namespace RealRadio.Components.Radio;

public class RadioStationInfoManager : PersistentSingleton<RadioStationInfoManager>
{
    public Action<Data.RadioStation, SongInfo>? SongInfoUpdated { get; set; }

    public SongInfoFetchManager SongInfoFetchManager { get; private set; } = null!;

    private readonly Dictionary<Data.RadioStation, ISongInfoFetcher> fetchers = [];

    private Dictionary<Data.RadioStation, List<SongInfo>> pendingUpdates = [];

    public override void Awake()
    {
        base.Awake();

        SongInfoFetchManager = new SongInfoFetchManager();
        SongInfoFetchManager.AddGlobalPlayerFetcher();
        SongInfoFetchManager.AddOneFMFetcher();
        SongInfoFetchManager.AddSimulatorRadioFetcher();
        SongInfoFetchManager.AddTruckersFMSongInfoFetcher();

        foreach (var station in RadioStationManager.Instance.Stations)
            OnRadioStationAdded(station);

        RadioStationManager.Instance.StationAdded += OnRadioStationAdded;
        RadioStationManager.Instance.StationRemoved += OnRadioStationRemoved;
    }

    private void Update()
    {
        lock (pendingUpdates)
        {
            foreach (var (station, list) in pendingUpdates)
            {
                if (list == null)
                    continue;

                foreach (var songInfo in list)
                {
                    Plugin.Logger.LogInfo($"Updating song info for radio station '{station.Name}': {songInfo}");
                    SongInfoUpdated?.Invoke(station, songInfo);
                }

                list.Clear();
            }
        }
    }

    public async Task<SongInfo?> GetSong(Data.RadioStation station)
    {
        if (!fetchers.TryGetValue(station, out var fetcher))
            return null;

        try
        {
            return await fetcher.RequestSongInfo();
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"Failed to get song info for radio station '{station.Name}': {ex}");
            return null;
        }
    }

    private void OnRadioStationAdded(Data.RadioStation station)
    {
        if (station.Type != RadioType.InternetRadio)
            return;

        // todo: initialize fetcher
        StartCoroutine(AddFetcher(station));
    }

    private void OnRadioStationRemoved(Data.RadioStation station)
    {
        if (fetchers.Remove(station, out var fetcher))
        {
            StartCoroutine(RemoveFetcher(station));
        }
    }

    private IEnumerator AddFetcher(Data.RadioStation station)
    {
        var getFetcherTask = SongInfoFetchManager.GetFetcher(new Uri(station.Url));
        yield return new WaitUntil(() => getFetcherTask.IsCompleted);

        if (getFetcherTask.IsFaulted || getFetcherTask.Result == null)
        {
            if (getFetcherTask.IsFaulted)
                Plugin.Logger.LogError($"Failed to get fetcher for radio station '{station.Id} ({station.Url})':\n{getFetcherTask.Exception}");
            else
                Plugin.Logger.LogWarning($"No fetcher found for radio station '{station.Id} ({station.Url})'");

            yield break;
        }

        ISongInfoFetcher fetcher = getFetcherTask.Result;
        fetchers[station] = fetcher;

        fetcher.SubscribeToSongInfoChanges(songInfo =>
        {
            lock (pendingUpdates)
            {
                if (!pendingUpdates.TryGetValue(station, out var list))
                {
                    list = new List<SongInfo>();
                    pendingUpdates[station] = list;
                }

                list.Add(songInfo);
            }
        });

        fetcher.RequestSongInfo();
    }

    private IEnumerator RemoveFetcher(Data.RadioStation station)
    {
        var removeTask = SongInfoFetchManager.RemoveFetcher(new Uri(station.Url));
        yield return new WaitUntil(() => removeTask.IsCompleted);

        if (removeTask.IsFaulted)
            Plugin.Logger.LogError($"Failed to remove fetcher for radio station '{station.Id} ({station.Url})':\n{removeTask.Exception}");
    }
}
