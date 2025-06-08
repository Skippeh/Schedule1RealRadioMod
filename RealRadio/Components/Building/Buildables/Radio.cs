using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using HashUtility;
using RealRadio.Components.Audio;
using RealRadio.Components.Radio;
using RealRadio.Data;
using RealRadio.Events;
using RealRadio.Persistence.Data;
using ScheduleOne;
using ScheduleOne.Audio;
using ScheduleOne.Dialogue;
using ScheduleOne.Interaction;
using ScheduleOne.Management;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using ScheduleOne.UI.Compass;
using UnityEngine;

namespace RealRadio.Components.Building.Buildables;

public class Radio : TogglableOffGridItem, IUsable
{
    public RadioStation? RadioStation { get; private set; }

    [field: SyncVar(Channel = Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ClientUnsynchronized, OnChange = nameof(OnStationChanged))]
    public uint? RadioStationIdHash { get; private set; }

    [field: SyncVar(Channel = Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnVolumeChanged))]
    public float Volume { get; set; }

    public GameObject AudioClientObject = null!;

    public Transform ConfigureCameraTransform = null!;

    protected StreamAudioClient? audioClient;
    protected CrossfadeAudioSources crossFade = null!;

    private InteractableOptions? interactableOptions;
    private InteractableObject? interactableObject;

    [field: SyncVar(Channel = Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    public NetworkObject? NPCUserObject { get; set; }

    [field: SyncVar(Channel = Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnPlayerUserChanged))]
    public NetworkObject? PlayerUserObject { get; set; }

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
            Plugin.Logger.LogWarning($"Invalid radio station hash (not found): {idHash}");
            return;
        }

        RadioStationIdHash = idHash;
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    public void SetNPCUser(NetworkObject? npcObject)
    {
        NPCUserObject = npcObject;
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    public void SetPlayerUser(NetworkObject? playerObject)
    {
        PlayerUserObject = playerObject;
    }

    protected virtual void OnPlayerUserChanged(NetworkObject prev, NetworkObject next, bool asServer)
    {
    }

    public override BuildableItemData GetBaseData()
    {
        return new RadioData(
            GUID,
            ItemInstance,
            loadOrder: 0,
            IsOn,
            transform.position,
            transform.rotation.eulerAngles,
            RadioStation?.Id!.GetStableHashCode(),
            Volume
        );
    }

    public override void Awake()
    {
        base.Awake();

        if (isGhost)
        {
            Destroy(AudioClientObject);
            return;
        }

        if (AudioClientObject == null)
            throw new InvalidOperationException("AudioClientObject is null");

        crossFade = AudioClientObject.GetComponent<CrossfadeAudioSources>() ?? throw new InvalidOperationException("No CrossfadeAudioSources component found on AudioClientObject");

        if (ConfigureCameraTransform == null)
            throw new InvalidOperationException("ConfigureCameraTransform is null");

        interactableObject = GetComponentInChildren<InteractableObject>() ?? throw new InvalidOperationException("No InteractableObject component found in self or children");
        interactableOptions = GetComponentInChildren<InteractableOptions>() ?? throw new InvalidOperationException("No InteractableOptions component found in self or children");
        interactableOptions.OnInteract += OnInteract;
        interactableOptions.OnUpdateInteractionText += OnUpdateInteractionText;

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
            Plugin.Logger.LogInfo($"Stopping radio because the station was removed");
            SetRadioStationIdHash(null);
        }
    }

    public virtual void Update()
    {
        if (PlayerUserObject == Player.Local.NetworkObject)
        {
            if (GameInput.GetButtonDown(GameInput.ButtonCode.Escape))
            {
                StopConfiguring();
            }
        }
    }

    private void OnUpdateInteractionText(InteractableOption? option, EventRefData<string> data)
    {
        if (option?.Id == "toggle")
            data.Value = IsOn ? "Turn off" : "Turn on";
    }

    private void OnInteract(string optionId)
    {
        switch (optionId)
        {
            case "toggle":
                IsOn = !IsOn;
                break;
            case "configure":
                StartConfigureIfPossible();
                break;
            default:
                Plugin.Logger.LogWarning($"Unknown option id: {optionId}");
                break;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        RadioStationIdHash = 0; // temp for testing
        OnStationChanged(null, RadioStationIdHash, true);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnStationChanged(0, RadioStationIdHash, false);
    }

    protected override void OnStateToggled(bool prev, bool next, bool asServer)
    {
        base.OnStateToggled(prev, next, asServer);

        if (asServer || isGhost)
            return;

        if (next && RadioStation != null)
        {
            InitAudioClient();
        }
        else
        {
            if (audioClient != null)
                audioClient.gameObject.SetActive(false);
        }
    }

    protected virtual void OnStationChanged(uint? prev, uint? next, bool asServer)
    {
        var prevStation = prev == null ? null : RadioStationManager.Instance.StationsByHashedId.GetValueOrDefault(prev.Value);
        var nextStation = next == null ? null : RadioStationManager.Instance.StationsByHashedId.GetValueOrDefault(next.Value);

        if (nextStation == RadioStation)
            return;

        if (RadioStation != null)
            UnbindAudioClient();

        RadioStation = nextStation;

        if (RadioStation != null)
            InitAudioClient();
    }

    protected virtual void OnVolumeChanged(float prev, float next, bool asServer)
    {
        if (asServer)
            return;

        crossFade.Volume = Mathf.Clamp01(next);
    }

    private void StartConfigureIfPossible()
    {
        if (((IUsable)this).IsInUse)
            return;

        Plugin.Logger.LogInfo("Start configure radio");
        SetPlayerUser(Player.Local.NetworkObject);
        OnStartConfigure();
    }

    private void StopConfiguring()
    {
        Plugin.Logger.LogInfo("Stop configure radio");
        OnEndConfigure();
        SetPlayerUser(null);
    }

    protected virtual void OnStartConfigure()
    {
        PlayerCamera.Instance.AddActiveUIElement(name);
        PlayerCamera.Instance.FreeMouse();
        PlayerCamera.Instance.OverrideTransform(ConfigureCameraTransform.position, ConfigureCameraTransform.rotation, lerpTime: 0.2f);
        PlayerCamera.Instance.OverrideFOV(60f, 0.2f);
        PlayerInventory.Instance.SetInventoryEnabled(false);
        PlayerMovement.Instance.canMove = false;
        CompassManager.Instance.SetVisible(false);
    }

    protected virtual void OnEndConfigure()
    {
        PlayerCamera.Instance.RemoveActiveUIElement(name);
        PlayerCamera.Instance.LockMouse();
        PlayerCamera.Instance.StopFOVOverride(0.2f);
        PlayerCamera.Instance.StopTransformOverride(0.2f);
        PlayerInventory.Instance.SetInventoryEnabled(true);
        PlayerMovement.Instance.canMove = true;
        CompassManager.Instance.SetVisible(true);
    }

    private void InitAudioClient()
    {
        if (RadioStation?.Url == null)
            throw new InvalidOperationException("Can not init. RadioStation or RadioStation.Url is null");

        if (IsOn)
            AudioClientObject.SetActive(true);
        else
            AudioClientObject.SetActive(false);

        if (audioClient == null)
        {
            audioClient = AudioStreamManager.Instance.GetOrCreateHost(RadioStation).AddClient(AudioClientObject);
            audioClient.ConvertToMono = true;
        }
    }

    private void UnbindAudioClient()
    {
        if (RadioStation?.Url == null)
            throw new InvalidOperationException("Can not unbind. RadioStation or RadioStation.Url is null");

        if (audioClient?.Host != null)
        {
            audioClient.Host.DetachClient(AudioClientObject);
        }

        audioClient = null;
    }
}
