using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RealRadio.Components.YoutubeDL;
using RealRadio.Data;
using ScheduleOne.DevUtilities;
using SongInfoFetcher;
using SongInfoFetcher.GlobalPlayer;
using SongInfoFetcher.OneFM;
using SongInfoFetcher.SimulatorRadio;
using SongInfoFetcher.TruckersFM;
using UnityEngine;
using YoutubeDLSharp.Metadata;

namespace RealRadio.Components.Radio;

public class RadioStationInfoManager : PersistentSingleton<RadioStationInfoManager>
{
    public static Action<SongInfoFetchManager>? RegisterFetchers;

    public Action<RadioStation, SongInfo>? SongInfoUpdated { get; set; }

    public SongInfoFetchManager SongInfoFetchManager { get; private set; } = null!;

    private readonly Dictionary<RadioStation, ISongInfoFetcher> fetchers = [];
    private readonly Dictionary<RadioStation, ManualSongInfoFetcher> manualFetchers = [];
    private Dictionary<RadioStation, List<SongInfo>> pendingUpdates = [];
    private readonly Dictionary<ISongInfoFetcher, float> fetchersToPoll = [];
    private readonly HashSet<ISongInfoFetcher> updatedPollTimes = [];
    private bool subscribedToSyncManager;

    public override void Awake()
    {
        base.Awake();
        SongInfoFetchManager = new SongInfoFetchManager();
    }

    public override void Start()
    {
        base.Start();

        SongInfoFetchManager.AddGlobalPlayerFetcher();
        SongInfoFetchManager.AddOneFMFetcher();
        SongInfoFetchManager.AddSimulatorRadioFetcher();
        SongInfoFetchManager.AddTruckersFMSongInfoFetcher();
        RegisterFetchers?.Invoke(SongInfoFetchManager);

        foreach (var station in RadioStationManager.Instance.Stations)
            OnRadioStationUpdated(station, oldStation: null);

        RadioStationManager.Instance.StationUpdated += OnRadioStationUpdated;
        RadioStationManager.Instance.StationRemoved += OnRadioStationRemoved;
    }

    private void Update()
    {
        if (!subscribedToSyncManager && RadioSyncManager.InstanceExists)
        {
            foreach (var state in RadioSyncManager.Instance.RadioStates.Where(kv => kv.Value.IsValid()))
                OnRadioStationSyncStateReceived(state.Key, state.Value);

            RadioSyncManager.Instance.OnStateReceived += OnRadioStationSyncStateReceived;
            subscribedToSyncManager = true;
        }
        else if (subscribedToSyncManager && !RadioSyncManager.InstanceExists)
        {
            subscribedToSyncManager = false;
        }

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

    private void OnRadioStationSyncStateReceived(RadioStation station, RadioStationState state)
    {
        if (!manualFetchers.TryGetValue(station, out var fetcher))
            return;

        fetcher.CurrentSong = GetSongFromState(station, state);
    }

    /// <summary>
    /// Requests song info for the specified radio station.
    /// </summary>
    public async Task<SongInfo?> RequestSong(RadioStation station)
    {
        if (!fetchers.TryGetValue(station, out var fetcher))
            return null;

        try
        {
            return await fetcher.RequestSongInfo();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to get song info for radio station '{station.Name}': {ex}");
            return null;
        }
    }

    /// <summary>
    /// Returns the current cached song info for the specified radio station. If no song info is available, returns null.
    /// Use <see cref="RequestSong(RadioStation)"/> to request song info.
    /// /// </summary>
    public SongInfo? GetSong(RadioStation station) => fetchers.TryGetValue(station, out var fetcher) ? fetcher.CurrentSong : null;

    private void OnRadioStationUpdated(RadioStation station, RadioStation? oldStation)
    {
        Logger.LogDebug($"Updating song info fetcher for radio station '{station}'...");

        StartCoroutine(UpdateFetcher(station, oldStation));
    }

    private IEnumerator UpdateFetcher(RadioStation station, RadioStation? oldStation)
    {
        if (oldStation != null)
            yield return RemoveFetcher(oldStation);

        switch (station.Type)
        {
            case RadioType.InternetRadio:
                yield return AddInternetRadioFetcher(station);
                break;
            case RadioType.YtDlp:
                AddYtDlpRadioFetcher(station);
                break;
            default:
                Logger.LogWarning($"Can not create song info fetcher due to unexpected radio type: {station.Type}");
                break;
        }
    }

    private void OnRadioStationRemoved(RadioStation station)
    {
        StartCoroutine(RemoveFetcher(station));
    }

    private IEnumerator AddInternetRadioFetcher(RadioStation station)
    {
        var getFetcherTask = SongInfoFetchManager.GetFetcher(new Uri(station.Url));
        yield return new WaitUntil(() => getFetcherTask.IsCompleted);

        if (getFetcherTask.IsFaulted || getFetcherTask.Result == null)
        {
            if (getFetcherTask.IsFaulted)
                Logger.LogError($"Failed to get song info fetcher for radio station '{station.Id} ({station.Url})':\n{getFetcherTask.Exception}");
            else
                Logger.LogWarning($"No song info fetcher found for radio station '{station.Id} ({station.Url})'");

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

    private void AddYtDlpRadioFetcher(RadioStation station)
    {
        var fetcher = new ManualSongInfoFetcher();
        fetchers[station] = fetcher;
        manualFetchers[station] = fetcher;

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

        var state = RadioSyncManager.Instance?.GetLocalState(station);

        if (state != null)
            fetcher.CurrentSong = GetSongFromState(station, state);
    }

    private IEnumerator RemoveFetcher(RadioStation station)
    {
        Logger.LogDebug($"Removing song info fetcher for radio station '{station.Id} ({station.Url})'...");

        fetchers.Remove(station, out var fetcher);
        pendingUpdates.Remove(station);
        updatedPollTimes.Remove(fetcher);

        // if the fetcher is not in the manual fetchers, it was added to the song info fetch manager
        if (!manualFetchers.Remove(station))
        {
            var removeTask = SongInfoFetchManager.RemoveFetcher(new Uri(station.Url));
            yield return new WaitUntil(() => removeTask.IsCompleted);

            if (removeTask.IsFaulted)
                Logger.LogError($"Failed to remove song info fetcher for radio station '{station.Id} ({station.Url})':\n{removeTask.Exception}");
        }
    }

    private SongInfo? GetSongFromState(RadioStation station, RadioStationState state)
    {
        if (!state.IsValid())
            return null;

        if (station.Type != RadioType.YtDlp || station.Urls == null || station.Urls.Length < state.SongIndex)
            return null;

        string url = station.Urls[state.SongIndex.Value];

        if (!YtDlpManager.Instance.AudioMetaData.TryGetValue(url, out var metaData))
            return null;

        return SongInfoFromVideoData(metaData);
    }

    public static SongInfo SongInfoFromVideoData(VideoData metaData)
    {
        string? title = metaData.Title;
        int indexOfDash = title.IndexOf('-');

        if (indexOfDash == -1)
            return new SongInfo(title, metaData.Uploader);

        string artistName = title.Substring(0, indexOfDash).Trim();
        title = indexOfDash >= title.Length - 1 ? null : title.Substring(indexOfDash + 1).Trim();

        return new SongInfo(title, artistName);
    }
}
