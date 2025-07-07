using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using RealRadio.Components.Audio;
using RealRadio.Data;
using ScheduleOne;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.Interaction;
using ScheduleOne.Management;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Compass;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealRadio.Components.Building.Buildables;

public class Speaker : OffGridItem, IUsable
{
    public Radio? Master { get; private set; }

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly)]
    public NetworkObject? NPCUserObject { get; set; }

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnPlayerUserChanged))]
    public NetworkObject? PlayerUserObject { get; set; }

    [SerializeField] private StreamAudioClient audioClient = null!;
    [SerializeField] private AudioSourceController audioSourceController = null!;
    [SerializeField] private SpeakerConfigureUI configureUI = null!;
    [SerializeField] private Transform configureCameraTransform = null!;

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnMasterGuidChanged))]
    private Guid? masterGuid;

    private InteractableOptions interactableOptions = null!;

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

        interactableOptions = GetComponentInChildren<InteractableOptions>() ?? throw new InvalidOperationException("No InteractableObject component found in self or children");
        interactableOptions.OnInteract += OnInteract;

        audioClient.ConvertToMono = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMaster(Radio? master)
    {
        masterGuid = master?.GUID;
    }

    private void OnMasterGuidChanged(Guid? prev, Guid? next, bool asServer)
    {
        if (asServer)
            return;

        var oldMaster = Master;

        if (next == null)
            Master = null;
        else
        {
            Master = GUIDManager.GetObject<Radio>(next.Value);

            if (Master == null)
                throw new ArgumentException($"Could not find radio with guid: {next.Value}");
        }

        if (!ReferenceEquals(oldMaster, Master))
            OnMasterChanged(oldMaster, Master);
    }

    private void OnMasterChanged(Radio? prev, Radio? next)
    {
        if (!ReferenceEquals(prev, null))
            UnbindFromMaster(prev);

        if (next != null)
            BindToMaster(next);
    }

    protected virtual void OnPlayerUserChanged(NetworkObject? prev, NetworkObject? next, bool asServer)
    {
        if (asServer)
            return;

        if (next == Player.Local.NetworkObject)
        {
            configureUI.gameObject.SetActive(true);
            OnStartConfigure();
        }
        else
        {
            configureUI.gameObject.SetActive(false);

            if (prev == Player.Local.NetworkObject)
                OnEndConfigure();
        }
    }

    private void OnInteract(string id)
    {
        if (id == "configure")
            StartConfigureIfPossible();
    }

    private void StartConfigureIfPossible()
    {
        if (((IUsable)this).IsInUse)
            return;

        Plugin.Logger.LogInfo("Start configure speaker");
        SetPlayerUser(Player.Local.NetworkObject);
    }

    public void StopConfiguring()
    {
        if (PlayerUserObject != Player.Local.NetworkObject)
            return;

        Plugin.Logger.LogInfo("Stop configure speaker");
        SetPlayerUser(null);
    }

    protected virtual void OnStartConfigure()
    {
        PlayerCamera.Instance.AddActiveUIElement(name);
        PlayerCamera.Instance.FreeMouse();
        Vector3 configureRotation = configureCameraTransform.rotation.eulerAngles;
        configureRotation.z = 0;
        PlayerCamera.Instance.OverrideTransform(configureCameraTransform.position, Quaternion.Euler(configureRotation), lerpTime: 0.2f);
        PlayerCamera.Instance.OverrideFOV(60f, 0.2f);
        PlayerInventory.Instance.SetInventoryEnabled(false);
        PlayerMovement.Instance.canMove = false;
        CompassManager.Instance.SetVisible(false);
    }

    protected virtual void OnEndConfigure()
    {
        PlayerCamera.Instance.RemoveActiveUIElement(name);
        PlayerCamera.Instance.LockMouse();
        PlayerCamera.Instance.StopTransformOverride(0.2f);
        PlayerCamera.Instance.StopFOVOverride(0.2f);
        PlayerInventory.Instance.SetInventoryEnabled(true);
        PlayerMovement.Instance.canMove = true;
        CompassManager.Instance.SetVisible(true);
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
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (Master == null)
            {
                // Debug: find first small portable radio and set it as master
                var smallPortableRadio = FindObjectOfType<SmallPortableRadio>();

                if (smallPortableRadio != null)
                    SetMaster(smallPortableRadio.GetComponent<Radio>());
            }
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
}

public class SpeakerConfigureUI : MonoBehaviour
{
    [SerializeField] private UIDocument document = null!;

    private Speaker speaker = null!;

    void Awake()
    {
        if (document == null)
            throw new InvalidOperationException("No UIDocument component found on game object");

        speaker = GetComponentInParent<Speaker>() ?? throw new InvalidOperationException("No Speaker component found in self or parents");
    }

    void OnEnable()
    {
        GameInput.RegisterExitListener(OnExitInput);
    }

    void OnDisable()
    {
        GameInput.DeregisterExitListener(OnExitInput);
    }

    private void OnExitInput(ExitAction exitAction)
    {
        if (exitAction.Used || speaker.PlayerUserObject != Player.Local.NetworkObject)
            return;

        exitAction.Used = true;
        speaker.StopConfiguring();
    }
}
