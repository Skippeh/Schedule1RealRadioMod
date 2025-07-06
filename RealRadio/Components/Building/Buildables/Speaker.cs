using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using RealRadio.Components.Audio;
using RealRadio.Data;
using ScheduleOne.Audio;
using UnityEngine;

namespace RealRadio.Components.Building.Buildables;

public class Speaker : OffGridItem
{
    public Radio? Master { get; private set; }

    [SerializeField] private StreamAudioClient audioClient = null!;
    [SerializeField] private AudioSourceController audioSourceController = null!;

    [field: SyncVar(Channel = FishNet.Transporting.Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnMasterGuidChanged))]
    private Guid? masterGuid;

    override public void Awake()
    {
        base.Awake();

        if (audioClient == null)
            throw new InvalidOperationException("AudioClient is null");

        if (audioSourceController == null)
            throw new InvalidOperationException("AudioSourceController is null");

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

        try
        {
            if (!ReferenceEquals(oldMaster, Master))
                OnMasterChanged(oldMaster, Master);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError(ex);
        }
    }

    private void OnMasterChanged(Radio? prev, Radio? next)
    {
        if (!ReferenceEquals(prev, null))
            UnbindFromMaster(prev);

        if (next != null)
            BindToMaster(next);
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
}
