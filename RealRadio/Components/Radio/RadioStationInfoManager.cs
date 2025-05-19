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
    public static Action<SongInfoFetchManager>? RegisterFetchers;

    public Action<Data.RadioStation, SongInfo>? SongInfoUpdated { get; set; }

    public SongInfoFetchManager SongInfoFetchManager { get; private set; } = null!;

    private readonly Dictionary<Data.RadioStation, ISongInfoFetcher> fetchers = [];
    private Dictionary<Data.RadioStation, List<SongInfo>> pendingUpdates = [];
    private readonly Dictionary<ISongInfoFetcher, float> fetchersToPoll = [];
    private readonly HashSet<ISongInfoFetcher> updatedPollTimes = [];

    public override void Awake()
    {
        base.Awake();

        SongInfoFetchManager = new SongInfoFetchManager();
        SongInfoFetchManager.AddGlobalPlayerFetcher();
        SongInfoFetchManager.AddOneFMFetcher();
        SongInfoFetchManager.AddSimulatorRadioFetcher();
        SongInfoFetchManager.AddTruckersFMSongInfoFetcher();
        RegisterFetchers?.Invoke(SongInfoFetchManager);

        foreach (var station in RadioStationManager.Instance.Stations)
            OnRadioStationAdded(station);

        RadioStationManager.Instance.StationAdded += OnRadioStationAdded;
        RadioStationManager.Instance.StationRemoved += OnRadioStationRemoved;
    }

    private void Update()
    {
        updatedPollTimes.Clear();

        foreach (var kv in fetchersToPoll)
        {
            if (Time.time - kv.Value >= 10f)
            {
                updatedPollTimes.Add(kv.Key);
                kv.Key.RequestSongInfo();
            }
        }

        float currentTime = Time.unscaledTime;

        foreach (var fetcher in updatedPollTimes)
            fetchersToPoll[fetcher] = currentTime;

        lock (pendingUpdates)
        {
            foreach (var (station, list) in pendingUpdates)
            {
                if (list == null)
                    continue;

                foreach (var songInfo in list)
                {
                    SongInfoUpdated?.Invoke(station, songInfo);
                }

                list.Clear();
            }
        }
    }

    /// <summary>
    /// Requests song info for the specified radio station.
    /// </summary>
    public async Task<SongInfo?> RequestSong(Data.RadioStation station)
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

    /// <summary>
    /// Returns the current cached song info for the specified radio station. If no song info is available, returns null.
    /// Use <see cref="RequestSong(Data.RadioStation)"/> to request song info.
    /// /// </summary>
    public SongInfo? GetSong(Data.RadioStation station) => fetchers.TryGetValue(station, out var fetcher) ? fetcher.CurrentSong : null;

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
                Plugin.Logger.LogError($"Failed to get song info fetcher for radio station '{station.Id} ({station.Url})':\n{getFetcherTask.Exception}");
            else
                Plugin.Logger.LogWarning($"No song info fetcher found for radio station '{station.Id} ({station.Url})'");

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

        if (!fetcher.CanListenForSongInfo && fetcher.CanRequestSongInfo)
            fetchersToPoll.Add(fetcher, Time.unscaledTime);

        if (fetcher.CanRequestSongInfo)
            fetcher.RequestSongInfo();
    }

    private IEnumerator RemoveFetcher(Data.RadioStation station)
    {
        var removeTask = SongInfoFetchManager.RemoveFetcher(new Uri(station.Url));
        yield return new WaitUntil(() => removeTask.IsCompleted);

        if (removeTask.IsFaulted)
            Plugin.Logger.LogError($"Failed to remove song info fetcher for radio station '{station.Id} ({station.Url})':\n{removeTask.Exception}");
    }
}
