using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using HashUtility;
using RealRadio.Components.Audio;
using RealRadio.Data;
using UnityEngine;

namespace RealRadio.Components.Radio;

public abstract class RadioProxy : NetworkBehaviour
{
    [field: SerializeField]
    public GameObject AudioClientPrefab { get; private set; } = null!;

    public RadioStation? RadioStation { get; private set; }

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ClientUnsynchronized, OnChange = nameof(OnStationChanged))]
    public uint? RadioStationIdHash { get; private set; }

    protected GameObject? audioClientObject;
    protected StreamAudioClient? audioClient;
    protected AudioSource? audioSource;

    protected virtual void Awake()
    {
        if (AudioClientPrefab == null)
            throw new InvalidOperationException("AudioClientPrefab is null");

        RadioStationManager.Instance.StationUpdated += OnRadioStationUpdated;
        RadioStationManager.Instance.StationRemoved += OnRadioStationRemoved;
    }

    private void OnRadioStationUpdated(RadioStation station, RadioStation? oldStation)
    {
        if (RadioStationIdHash == null)
            return;

        // The latter condition is to check if the station was set before user stations were received if we're not the server.
        // This fixes stations being unknown on the client if they're already playing when the client joins the game.
        if (station.Id?.GetStableHashCode() != RadioStationIdHash)
            return;

        RadioStation = station;
    }

    private void OnRadioStationRemoved(RadioStation station)
    {
        if (!IsServer)
            return;

        if (RadioStation == station)
        {
            Logger.LogDebug($"Stopping radio because the station was removed");
            SetRadioStationIdHash(null);
        }
    }

    protected virtual void OnDestroy()
    {
        if (audioClientObject)
            Destroy(audioClientObject);
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    public void SetRadioStationIdHash(uint? idHash)
    {
        if (idHash == null)
        {
            RadioStationIdHash = null;
            return;
        }

        if (!RadioStationManager.Instance.StationsByHashedId.TryGetValue(idHash.Value, out _))
        {
            Logger.LogWarning($"Invalid radio station hash (not found): {idHash}");
            return;
        }

        RadioStationIdHash = idHash;
    }

    private IEnumerator WaitForAudioClientThenEnable()
    {
        if (audioClientObject != null)
        {
            audioClientObject.SetActive(true);
            yield break;
        }

        yield return new WaitUntil(() => audioClientObject != null);
        audioClientObject!.SetActive(true);
    }

    protected virtual void OnStationChanged(uint? prev, uint? next, bool asServer)
    {
        if (asServer)
            return;

        RadioStation? nextStation = next == null ? null : RadioStationManager.Instance.StationsByHashedId.GetValueOrDefault(next.Value);

        if (RadioStation != null)
            UnbindAudioClient();

        RadioStation = nextStation;

        if (RadioStation != null)
        {
            InitAudioClient();
        }
    }

    protected virtual void InitAudioClient(bool delayStart = true)
    {
        if (RadioStation?.Url == null)
            return;

        if (audioClient)
        {
            Logger.LogWarning("AudioClient is already running");
            return;
        }

        if (audioClientObject == null)
            throw new InvalidOperationException("AudioClientObject is null");

        audioClient = AudioStreamManager.Instance.GetOrCreateHost(RadioStation).AddClient(audioClientObject);
    }

    protected virtual void UnbindAudioClient()
    {
        if (RadioStation?.Url == null)
            throw new InvalidOperationException("Can not unbind. RadioStation or RadioStation.Url is null");

        if (audioClientObject == null)
            throw new InvalidOperationException("AudioClientObject is null");


        audioClient?.Host?.DetachClient(audioClientObject);
        audioClient = null;
    }
}
