using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FishNet.Connection;
using FishNet.Object;
using RealRadio.Components.YoutubeDL;
using RealRadio.Data;
using ScheduleOne.DevUtilities;
using UnityEngine;
using YoutubeDLSharp.Metadata;

namespace RealRadio.Components.Radio;

public class RadioSyncManager : NetworkSingleton<RadioSyncManager>
{
    public Action<RadioStation, RadioStationState>? OnStateReceived;

    private Dictionary<RadioStation, RadioStationState> radioStates = [];

    public override void Awake()
    {
        base.Awake();

        RadioStationManager.Instance.StationAdded += OnRadioStationAdded;
        foreach (var station in RadioStationManager.Instance.Stations)
        {
            OnRadioStationAdded(station);
        }
    }

    private void OnRadioStationAdded(RadioStation station)
    {
        if (station.Type == RadioType.InternetRadio)
            return;

        radioStates[station] = new RadioStationState();
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

        var existingState = radioStates[station];

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

    [TargetRpc(RunLocally = true)]
    [ObserversRpc(RunLocally = true)]
    public void ReceiveSongState(NetworkConnection? conn, RadioStation station, RadioStationState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        if (IsClientOnly)
        {
            Plugin.Logger.LogInfo($"Adding to current time: {NetworkManager.TimeManager.RoundTripTime / 1000f} sec(s)");
            state.CurrentTime += NetworkManager.TimeManager.RoundTripTime / 1000f;
            radioStates[station] = state;
        }

        OnStateReceived?.Invoke(station, state);
    }

    private void FixedUpdate()
    {
        foreach (var (_, state) in radioStates)
        {
            if (state.CurrentTime != null)
                state.CurrentTime += Time.fixedUnscaledDeltaTime;

    public static RadioStationState GetRandomRadioStationState(RadioStation Station, ushort? lastSongIndex, uint? iteration, float? startTime = null)
    {
        var result = new RadioStationState();
        ushort index;

        while (true)
        {
            index = (ushort)UnityEngine.Random.Range(0, Station.Urls!.Length);

            if (lastSongIndex != index || Station.Urls.Length <= 1)
                break;
        }

        result.SongIndex = index;
        result.SongIteration = iteration;

        if (startTime == null)
        {
            if (YtDlpManager.Instance.AudioMetaData.TryGetValue(Station.Urls[index], out var metaData) && metaData.Duration.HasValue)
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
