using System;
using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using RealRadio.Components.Audio;
using RealRadio.Data;
using RealRadio.Persistence.Loaders;
using ScheduleOne;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.Management;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using ScheduleOne.UI.Compass;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.Building.Buildables;

public class Speaker : OffGridItem, IUsable
{
    public event Action<Radio?>? MasterChanged;

    public Radio? Master { get; private set; }

    /// <summary>
    /// The audio channel to use for this speaker. Do not set this directly, otherwise the change won't be synced over network.
    /// Use <see cref="SetSelectedAudioChannel"/>
    /// </summary>
    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnSelectedAudioChannelChanged), SendRate = 0)]
    public AudioChannel SelectedAudioChannel { get; set; } = AudioChannel.Both;

    /// <summary>
    /// Whether or not this speaker outputs stereo audio. Do not set this directly, otherwise the change won't be synced over network.
    /// Use <see cref="SetStereoOutput"/>
    /// </summary>
    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnStereoOutputChanged), SendRate = 0)]
    public bool StereoOutput { get; set; } = false;

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    public NetworkObject? NPCUserObject { get; set; }

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnPlayerUserChanged), SendRate = 0)]
    public NetworkObject? PlayerUserObject { get; set; }

    /// <summary>
    /// The rotation of the speaker mount. Do not set this directly, otherwise the change won't be synced over network.
    /// Use <see cref="SetMountRotation"/>
    /// </summary>
    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, SendRate = 0.1f, OnChange = nameof(OnMountRotationChanged))]
    public Vector2 MountRotation { get; set; }

    [field: SerializeField] public Vector2 MountRotationLimitsX { get; private set; }
    [field: SerializeField] public Vector2 MountRotationLimitsY { get; private set; }

    [SerializeField] private StreamAudioClient audioClient = null!;
    [SerializeField] private AudioSourceController audioSourceController = null!;
    [SerializeField] private SpeakerConfigureUI configureUI = null!;
    [SerializeField] private Transform configureCameraTransform = null!;
    [SerializeField] private Transform mountTransform = null!;

    /// <summary>
    /// The Guid of the master radio. Do not set this directly. Use <see cref="SetMaster"/>.
    /// </summary>
    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnMasterGuidChanged), SendRate = 0)]
    public Guid? MasterGuid { get; set; }

    private InteractableOptions interactableOptions = null!;
    private Coroutine? findRadioCoroutine;
    private Coroutine? lerpMountAngleCoroutine;
    private bool configuringMountAngle;
    private Vector2 localMountRotation;

    override public void Awake()
    {
        base.Awake();

        if (audioClient == null)
            throw new InvalidOperationException("AudioClient is null");

        if (audioSourceController == null)
            throw new InvalidOperationException("AudioSourceController is null");

        if (configureUI == null)
            throw new InvalidOperationException("ConfigureUI is null");

        if (configureCameraTransform == null)
            throw new InvalidOperationException("ConfigureCameraTransform is null");

        if (mountTransform == null)
            throw new InvalidOperationException("MountTransform is null");

        interactableOptions = GetComponentInChildren<InteractableOptions>() ?? throw new InvalidOperationException("No InteractableObject component found in self or children");
        interactableOptions.OnInteract += OnInteract;

        configureUI.ChannelChanged += OnAudioChannelConfigured;
        configureUI.StereoOutputChanged += OnStereoOutputConfigured;
        configureUI.ConnectSpeakerClicked += OnConnectSpeakerClicked;
        configureUI.DisconnectSpeakerClicked += OnDisconnectSpeakerClicked;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        OnSelectedAudioChannelChanged(SelectedAudioChannel, SelectedAudioChannel, false);
        OnStereoOutputChanged(StereoOutput, StereoOutput, false);
        OnMasterGuidChanged(MasterGuid, MasterGuid, false);
        OnPlayerUserChanged(PlayerUserObject, PlayerUserObject, false);
        OnMountRotationChanged(MountRotation, MountRotation, false);
    }

    public override BuildableItemData GetBaseData()
    {
        return new SpeakerData(
            GUID,
            ItemInstance,
            loadOrder: 1, // this needs to be loaded after radios due to the implicit dependence
            transform.position,
            transform.rotation.eulerAngles,
            Master?.GUID,
            SelectedAudioChannel,
            StereoOutput,
            MountRotation
        );
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMaster(Radio? master)
    {
        MasterGuid = master?.GUID;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetSelectedAudioChannel(AudioChannel channel)
    {
        if (!Enum.IsDefined(typeof(AudioChannel), channel))
            throw new ArgumentException($"Invalid audio channel: {channel}");

        SelectedAudioChannel = channel;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetStereoOutput(bool stereoOutput)
    {
        StereoOutput = stereoOutput;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMountRotation(Vector2 mountRotation)
    {
        MountRotation = mountRotation;
    }

    private void OnMasterGuidChanged(Guid? prev, Guid? next, bool asServer)
    {
        if (asServer)
            return;

        var oldMaster = Master;

        if (next == null)
        {
            Master = null;

            if (!ReferenceEquals(oldMaster, Master))
                OnMasterChanged(oldMaster, Master);
        }
        else
        {
            if (IsServer)
            {
                Radio radio = GUIDManager.GetObject<Radio>(next.Value);

                if (radio == null)
                    throw new ArgumentException($"[SERVER] Could not find radio with guid: {next.Value}");

                Master = radio;

                if (!ReferenceEquals(oldMaster, Master))
                    OnMasterChanged(oldMaster, Master);
            }
            else
            {
                if (findRadioCoroutine != null)
                    StopCoroutine(findRadioCoroutine);

                findRadioCoroutine = StartCoroutine(WaitForRadioThenSetMaster());

                IEnumerator WaitForRadioThenSetMaster()
                {
                    // Max waiting time in seconds for radio to be found.
                    // This needs to be relatively high due to items being registered in the Start method on clients,
                    // which isn't called immediately when joining the server and depends on how long the client takes to receive network data from server.
                    const float MAX_WAIT_TIME = 30f;

                    float startTime = Time.unscaledTime;
                    Radio? radio = null;
                    yield return new WaitUntil(() => Time.unscaledTime - startTime > MAX_WAIT_TIME || (radio = GUIDManager.GetObject<Radio>(next.Value)) != null);

                    if (radio == null)
                        throw new ArgumentException($"[CLIENT] Could not find radio with guid: {next.Value}");

                    Master = radio;

                    if (!ReferenceEquals(oldMaster, Master))
                        OnMasterChanged(oldMaster, Master);
                }
            }
        }
    }

    private void OnMasterChanged(Radio? prev, Radio? next)
    {
        if (!ReferenceEquals(prev, null))
            UnbindFromMaster(prev);

        if (next != null)
            BindToMaster(next);

        MasterChanged?.Invoke(next);
    }

    private void OnSelectedAudioChannelChanged(AudioChannel prev, AudioChannel next, bool asServer)
    {
        if (asServer)
            return;

        audioClient.LeftChannelVolume = next is AudioChannel.Left or AudioChannel.Both ? 1f : 0f;
        audioClient.RightChannelVolume = next is AudioChannel.Right or AudioChannel.Both ? 1f : 0f;
    }

    private void OnStereoOutputChanged(bool prev, bool next, bool asServer)
    {
        if (asServer)
            return;

        audioClient.ConvertToMono = !next;
    }

    protected virtual void OnPlayerUserChanged(NetworkObject? prev, NetworkObject? next, bool asServer)
    {
        if (asServer)
            return;

        if (next != null && next == Player.Local?.NetworkObject)
        {
            OnStartConfigure();
        }
        else
        {
            if (Player.Local != null && prev == Player.Local?.NetworkObject)
                OnEndConfigure();
        }
    }

    private void OnMountRotationChanged(Vector2 prev, Vector2 next, bool asServer)
    {
        if (asServer)
            return;

        if (PlayerUserObject == null || PlayerUserObject != (Player.Local?.NetworkObject))
        {
            if (lerpMountAngleCoroutine != null)
                StopCoroutine(lerpMountAngleCoroutine);

            lerpMountAngleCoroutine = StartCoroutine(LerpRotation());

            IEnumerator LerpRotation()
            {
                float t = 0f;

                while (t < 0.1f)
                {
                    localMountRotation = Vector2.Lerp(prev, next, t * 10f);
                    mountTransform.localRotation = Quaternion.Euler(localMountRotation.x, localMountRotation.y, 0);
                    t += Time.deltaTime;
                    yield return null;
                }

                localMountRotation = next;
                mountTransform.localRotation = Quaternion.Euler(localMountRotation.x, localMountRotation.y, 0);

                lerpMountAngleCoroutine = null;
            }
        }
    }

    private void OnInteract(string id)
    {
        if (id == "configure")
        {
            configuringMountAngle = false;
            StartConfigureIfPossible();
        }
        else if (id == "configureMountAngle")
        {
            configuringMountAngle = true;
            StartConfigureIfPossible();
        }
        else if (id == "resetMountAngle")
        {
            SetMountRotation(Vector2.zero);
        }
    }

    private void StartConfigureIfPossible()
    {
        if (((IUsable)this).IsInUse)
            return;

        SetPlayerUser(Player.Local.NetworkObject);
    }

    public void StopConfiguring()
    {
        if (PlayerUserObject != Player.Local.NetworkObject)
            return;

        SetPlayerUser(null);
    }

    protected virtual void OnStartConfigure()
    {
        if (!configuringMountAngle)
        {
            configureUI.gameObject.SetActive(true);

            PlayerCamera.Instance.FreeMouse();
            Vector3 configureRotation = configureCameraTransform.rotation.eulerAngles;
            configureRotation.z = 0;
            PlayerCamera.Instance.OverrideTransform(configureCameraTransform.position, Quaternion.Euler(configureRotation), lerpTime: 0.2f);
            PlayerCamera.Instance.OverrideFOV(60f, 0.2f);
        }
        else
        {
            PlayerCamera.Instance.OverrideTransform(PlayerCamera.Instance.transform.position, PlayerCamera.Instance.transform.rotation, 0);
        }

        PlayerCamera.Instance.AddActiveUIElement(name);
        PlayerInventory.Instance.SetInventoryEnabled(false);
        PlayerMovement.Instance.canMove = false;
        CompassManager.Instance.SetVisible(false);
        HUD.Instance.SetCrosshairVisible(false);

        GameInput.RegisterExitListener(OnExitInput);
    }

    protected virtual void OnEndConfigure()
    {
        if (!configuringMountAngle)
        {
            configureUI.gameObject.SetActive(false);

            PlayerCamera.Instance.LockMouse();
            PlayerCamera.Instance.StopFOVOverride(0.2f);
            PlayerCamera.Instance.StopTransformOverride(0.2f);
        }
        else
        {
            PlayerCamera.Instance.StopTransformOverride(0);
        }

        PlayerCamera.Instance.RemoveActiveUIElement(name);
        PlayerMovement.Instance.canMove = true;
        HUD.Instance.SetCrosshairVisible(true);

        // Hack: This method is called after SpeakerConnectionManager is enabled (which disables these two) due to the PlayerUserObject SyncVar change invocation happening later.
        // Check if SpeakerConnectionManager is enabled and don't re-enable them if so.
        // todo: Find a better way that doesn't involve classes checking if edit mode is enabled to re-enable stuff.
        if (!SpeakerConnectionManager.Instance.EditModeEnabled)
        {
            PlayerInventory.Instance.SetInventoryEnabled(true);
            CompassManager.Instance.SetVisible(true);
        }

        configuringMountAngle = false;
    }

    private void OnExitInput(ExitAction exitAction)
    {
        if (exitAction.Used || PlayerUserObject != Player.Local.NetworkObject)
            return;

        exitAction.Used = true;
        StopConfiguring();

        GameInput.DeregisterExitListener(OnExitInput);
    }

    void OnEnable()
    {
        if (isGhost)
            return;

        if (Master != null)
            BindToMaster(Master);
    }

    void OnDisable()
    {
        if (isGhost)
            return;

        if (Master != null)
            UnbindFromMaster(Master);
    }

    void Update()
    {
        if (isGhost)
            return;

        UpdateMountAngle();
    }

    private void UpdateMountAngle()
    {
        if (PlayerUserObject != Player.Local.NetworkObject || !configuringMountAngle)
            return;

        var inputDelta = GameInput.MouseDelta;
        inputDelta *= 0.5f;

        var previousRotation = localMountRotation;
        var newRotation = localMountRotation;
        newRotation += new Vector2(inputDelta.y, -inputDelta.x);

        newRotation.x = Mathf.Clamp(newRotation.x, MountRotationLimitsX.x, MountRotationLimitsX.y);
        newRotation.y = Mathf.Clamp(newRotation.y, MountRotationLimitsY.x, MountRotationLimitsY.y);
        localMountRotation = newRotation;

        if (newRotation != previousRotation)
        {
            SetMountRotation(newRotation);
            mountTransform.localRotation = Quaternion.Euler(localMountRotation.x, localMountRotation.y, 0);
        }
    }

    private void BindToMaster(Radio master)
    {
        if (master == null)
            throw new InvalidOperationException("Master is null");

        master.VolumeChanged += OnVolumeChanged;
        master.Toggled += OnToggled;
        master.RadioStationChanged += OnRadioStationChanged;
        master.BeforeDestroy += OnMasterDestroyed;
        master.AddSpeaker(this);

        OnVolumeChanged(master.Volume);
        OnToggled(master.IsOn);
        OnRadioStationChanged(master.RadioStation);
    }

    private void UnbindFromMaster(Radio master)
    {
        if (ReferenceEquals(master, null))
            throw new InvalidOperationException("Master is null");

        if (master)
        {
            master.VolumeChanged -= OnVolumeChanged;
            master.Toggled -= OnToggled;
            master.RadioStationChanged -= OnRadioStationChanged;
            master.BeforeDestroy -= OnMasterDestroyed;
            master.RemoveSpeaker(this);
        }

        audioClient.Host?.DetachClient(audioClient.gameObject);
    }

    private void OnVolumeChanged(float volume)
    {
        audioSourceController.SetVolume(volume);
    }

    private void OnToggled(bool isOn)
    {
        UpdateAudioClientBinding();
    }

    private void OnRadioStationChanged(RadioStation? radioStation)
    {
        UpdateAudioClientBinding();
    }

    private void OnMasterDestroyed()
    {
        if (IsServer)
            SetMaster(null);
        else
        {
            var master = Master;
            Master = null;
            OnMasterChanged(master, null);
        }
    }

    private void OnAudioChannelConfigured(AudioChannel channel)
    {
        SetSelectedAudioChannel(channel);
    }

    private void OnStereoOutputConfigured(bool enabled)
    {
        SetStereoOutput(enabled);
    }

    private void OnConnectSpeakerClicked()
    {
        StopConfiguring();
        SpeakerConnectionManager.Instance.StartEditMode(this, SpeakerConnectedCallback);

        void SpeakerConnectedCallback(Speaker speaker, Radio radio)
        {
            SpeakerConnectionManager.Instance.StopEditMode();
        }
    }

    private void OnDisconnectSpeakerClicked()
    {
        SetMaster(null);
    }

    private void UpdateAudioClientBinding()
    {
        if (Master == null || !Master.IsOn || Master.RadioStation == null)
        {
            audioClient.Host?.DetachClient(audioClient.gameObject);
        }
        else
        {
            StreamAudioHost? masterAudioHost = Master.RadioStation == null ? null : AudioStreamManager.Instance.GetOrCreateHost(Master.RadioStation);

            if (audioClient.Host != masterAudioHost)
            {
                audioClient.Host?.DetachClient(audioClient.gameObject);
                masterAudioHost?.AddClient(audioClient.gameObject);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerUser(NetworkObject? playerObject)
    {
        PlayerUserObject = playerObject;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetNPCUser(NetworkObject? playerObject)
    {
        NPCUserObject = playerObject;
    }

    public enum AudioChannel
    {
        Both,
        Left,
        Right,
    }
}

public class SpeakerConfigureUI : MonoBehaviour
{
    public event Action<Speaker.AudioChannel>? ChannelChanged;
    public event Action<bool>? StereoOutputChanged;
    public event Action? ConnectSpeakerClicked;
    public event Action? DisconnectSpeakerClicked;

    [SerializeField] private UIDocument document = null!;

    private Speaker speaker = null!;

#nullable disable
    private RadioButtonGroup channelGroup;
    private Toggle stereoOutputToggle;
    private Button connectSpeakerButton;
    private Button disconnectSpeakerButton;
#nullable enable

    void Awake()
    {
        if (document == null)
            throw new InvalidOperationException("No UIDocument component found on game object");

        speaker = GetComponentInParent<Speaker>() ?? throw new InvalidOperationException("No Speaker component found in self or parents");
    }

    void OnEnable()
    {
        var root = document.rootVisualElement;
        channelGroup = root.Query<RadioButtonGroup>(name: "ChannelGroup").First() ?? throw new InvalidOperationException("Could not find channel group ui element");
        channelGroup.RegisterValueChangedCallback((e) => ChannelChanged?.Invoke((Speaker.AudioChannel)e.newValue));

        stereoOutputToggle = root.Query<Toggle>(name: "StereoOutput").First() ?? throw new InvalidOperationException("Could not find stereo output toggle ui element");
        stereoOutputToggle.RegisterValueChangedCallback((e) => StereoOutputChanged?.Invoke(e.newValue));

        connectSpeakerButton = root.Query<Button>(name: "ConnectSpeaker").First() ?? throw new InvalidOperationException("Could not find connect speaker button ui element");
        connectSpeakerButton.clicked += () => ConnectSpeakerClicked?.Invoke();

        disconnectSpeakerButton = root.Query<Button>(name: "DisconnectSpeaker").First() ?? throw new InvalidOperationException("Could not find disconnect speaker button ui element");
        disconnectSpeakerButton.clicked += () => DisconnectSpeakerClicked?.Invoke();

        channelGroup.value = (int)speaker.SelectedAudioChannel;
        stereoOutputToggle.value = speaker.StereoOutput;

        speaker.MasterChanged += OnMasterChanged;

        OnMasterChanged(speaker.Master);
    }

    void OnDisable()
    {
        speaker.MasterChanged -= OnMasterChanged;
    }

    private void OnMasterChanged(Radio? master)
    {
        disconnectSpeakerButton.SetEnabled(speaker.Master != null);
    }
}
