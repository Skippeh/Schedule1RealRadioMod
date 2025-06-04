using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using FishNet.Connection;
using FishNet.Object;
using HashUtility;
using RealRadio.Components.YoutubeDL;
using RealRadio.Data;
using ScheduleOne.DevUtilities;
using UnityEngine;
using YoutubeDLSharp.Metadata;

namespace RealRadio.Components.Radio;

public class RadioSyncManager : NetworkSingleton<RadioSyncManager>
{
    public Action<RadioStation, RadioStationState>? OnStateReceived;

    public ReadOnlyDictionary<RadioStation, RadioStationState> RadioStates = null!;

    private Dictionary<RadioStation, RadioStationState> radioStates = [];
    private Dictionary<RadioStation, VideoData?> currentMetaData = [];

    public override void Awake()
    {
        base.Awake();

        RadioStates = new(radioStates);
        RadioStationManager.Instance.StationRemoved += OnRadioStationRemoved;
        RadioStationManager.Instance.StationUpdated += OnRadioStationUpdated;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        RadioStationManager.Instance.StationRemoved -= OnRadioStationRemoved;
        RadioStationManager.Instance.StationUpdated -= OnRadioStationUpdated;
    }

    public override void OnStartServer()
    {
        foreach (var station in RadioStationManager.Instance.Stations)
        {
            OnRadioStationUpdated(station, oldStation: null);
        }
    }

    public override void OnStartClient()
    {
        if (!IsClientOnly)
            return;

        UserStationsManager.Instance.StationUpdated += OnClientUserStationUpdated;

        // Request song state for all yt-dlp radio stations
        foreach (var station in RadioStationManager.Instance.Stations)
        {
            if (station.Type != RadioType.YtDlp)
                continue;

            RequestOrSetSongState(station, new RadioStationState());
        }
    }

    private void OnClientUserStationUpdated(API.Data.RadioStation station, bool isNew)
    {
        if (station.Type != RadioType.YtDlp)
            return;

        if (!RadioStationManager.Instance.StationsByHashedId.TryGetValue(station.Id!.GetStableHashCode(), out var radioStation))
            return;

        RequestOrSetSongState(radioStation, new RadioStationState());
    }

    private void OnRadioStationUpdated(RadioStation station, RadioStation? oldStation)
    {
        if (station.Type == RadioType.InternetRadio)
            return;

        RadioStationState? oldState = null;

        if (oldStation != null)
        {
            radioStates.Remove(oldStation, out oldState);
            currentMetaData.Remove(oldStation);
        }

        if (!IsServer)
            return;

        if (oldState?.IsValid() == true)
        {
            // clone old state
            oldState = oldState with { };

            string? oldSong = oldStation!.Urls![oldState.SongIndex.Value];

            int newIndex = Array.IndexOf(station.Urls!, oldSong);
            oldState.SongIndex = newIndex >= 0 ? (ushort)newIndex : null;

            if (oldState.SongIndex == null)
            {
                oldState = GetRandomRadioStationState(station, lastSongIndex: null, oldState.SongIteration + 1, startTime: 0f);
            }
        }

        var state = oldState ?? GetRandomRadioStationState(station, lastSongIndex: null, iteration: 0, startTime: null);
        radioStates[station] = state;
        RequestOrSetSongState(station, state);
    }

    private void OnRadioStationRemoved(RadioStation station)
    {
        radioStates.Remove(station);
        currentMetaData.Remove(station);
    }

    public RadioStationState? GetLocalState(RadioStation station)
    {
        if (radioStates.TryGetValue(station, out var state))
            return state;

        return null;
    }

    /// <summary>
    /// Request or set the state of a radio station.
    /// If the existing state's song iteration is the same the existing one will be sent to the requesting client.
    /// Otherwise the new state will be set and broadcasted to all clients, including the one requesting.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestOrSetSongState(RadioStation station, RadioStationState newState, NetworkConnection? conn = null)
    {
        if (newState == null)
            throw new ArgumentNullException(nameof(newState));

        if (station.Type == RadioType.InternetRadio)
            throw new ArgumentException($"Can not set the song state of an internet radio station");

        radioStates.TryGetValue(station, out var existingState);

        if (existingState != null && existingState.IsValid() && (newState.SongIteration <= existingState.SongIteration || newState.SongIteration == null))
        {
            ReceiveSongState(conn, station, existingState);
            return;
        }

        if (newState.SongIteration == null)
        {
            // if this is the first time this station state has been requested set the iteration to 1,
            // otherwise increment it
            if (existingState?.SongIteration == null)
                newState.SongIteration = 1;
            else
                newState.SongIteration = existingState.SongIteration.Value + 1;
        }

        if (newState.SongIndex >= station.Urls!.Length)
        {
            throw new ArgumentException($"Received out of bounds song index: {newState.SongIndex} (max {station.Urls.Length - 1})");
        }

        radioStates[station] = newState;

        ReceiveSongState(conn: null, station, newState);
    }

    // OrderType is important here.
    // If we don't set it the song state will be received before the station is sent (if a runtime station was added by a client)
    [TargetRpc(RunLocally = true, OrderType = DataOrderType.Last)]
    [ObserversRpc(RunLocally = true, OrderType = DataOrderType.Last)]
    public void ReceiveSongState(NetworkConnection? conn, RadioStation station, RadioStationState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        if (IsClientOnly)
        {
            state.CurrentTime += NetworkManager.TimeManager.RoundTripTime / 2f / 1000f;
            radioStates[station] = state;
        }

        VideoData? videoData = null;

        if (station.Type == RadioType.YtDlp && station.Urls != null && station.Urls.Length > state.SongIndex)
        {
            string url = station.Urls[state.SongIndex.Value];

            if (YtDlpManager.Instance.AudioMetaData.TryGetValue(url, out var metaData))
                videoData = metaData;
        }

        currentMetaData[station] = videoData;
        OnStateReceived?.Invoke(station, state);
    }

    private void FixedUpdate()
    {
        foreach (var (station, state) in radioStates)
        {
            if (state.CurrentTime != null)
                state.CurrentTime += Time.fixedUnscaledDeltaTime;

            currentMetaData.TryGetValue(station, out VideoData? metaData);

            // If the song's time has passed the end of song + 2 seconds then switch to the next song
            // We add 2 seconds because normally if the station is actively being listened to it'll switch to the next song immediately.
            // If the station is not actively being listened the state's time will go past the end of the song.
            // When doing it this way we ensure that the song isn't cut off early if the duration is not completely accurate.
            if (metaData?.Duration != null && state.CurrentTime > metaData.Duration + 2f)
            {
                var newState = GetRandomRadioStationState(station, state.SongIndex, state.SongIteration + 1, startTime: state.SongIteration == null ? null : 0);
                RequestOrSetSongState(station, newState);
            }
        }
    }

    public static RadioStationState GetRandomRadioStationState(RadioStation station, ushort? lastSongIndex, uint? iteration, float? startTime = null)
    {
        var result = new RadioStationState();
        ushort index;

        while (true)
        {
            index = (ushort)UnityEngine.Random.Range(0, station.Urls!.Length);

            if (lastSongIndex != index || station.Urls.Length <= 1)
                break;
        }

        result.SongIndex = index;
        result.SongIteration = iteration;

        if (startTime == null)
        {
            if (YtDlpManager.Instance.AudioMetaData.TryGetValue(station.Urls[index], out var metaData) && metaData.Duration.HasValue)
                startTime = UnityEngine.Random.Range(Math.Min(10f, metaData.Duration.Value), metaData.Duration.Value);
            else
            {
                // if we don't have metadata, assume the song is atleast 30 seconds long and pick a random time in that range.
                // if the song is shorter than 30 seconds it'll effectively be skipped, but that's fine for this rare case where the song is short AND the song isn't downloaded yet
                startTime = UnityEngine.Random.Range(10f, 30f);
            }
        }

        result.CurrentTime = startTime ?? 0;

        return result;
    }
}

public record class RadioStationState
{
    public uint? SongIteration;
    public ushort? SongIndex;
    public float? CurrentTime;

    /// <summary>
    /// Checks if the state is valid. The state is valid if all fields have non null values.
    /// </summary>
    [MemberNotNullWhen(true, nameof(SongIteration), nameof(SongIndex), nameof(CurrentTime))]
    public bool IsValid()
    {
        return SongIteration != null && SongIndex != null && CurrentTime != null;
    }
}
