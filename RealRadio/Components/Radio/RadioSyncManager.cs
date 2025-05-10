using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FishNet.Connection;
using FishNet.Object;
using RealRadio.Components.Radio;
using RealRadio.Data;
using ScheduleOne.DevUtilities;
using UnityEngine;

namespace RealRadio.Components.Audio;

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

        if (existingState != null && newState.SongIteration == existingState.SongIteration)
        {
            ReceiveSongState(conn, station, existingState);
            return;
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
            radioStates[station] = state;

        OnStateReceived?.Invoke(station, state);
    }

    private void Update()
    {
        if (!IsServer)
            return;

        foreach (var (_, state) in radioStates)
        {
            if (state.CurrentTime != null)
                state.CurrentTime += Time.deltaTime;
        }
    }
}

public record class RadioStationState
{
    public uint? SongIteration;
    public ushort? SongIndex;
    public float? CurrentTime;
}
