using System;
using RealRadio.Components.Audio;
using RealRadio.Data;
using ScheduleOne.Audio;
using UnityEngine;

namespace RealRadio.Components.Building.Buildables;

public class Speaker : OffGridItem
{
    public Radio? Master
    {
        get => master;
        set
        {
            if (master == value)
                return;

            if (master != null)
                UnbindFromMaster();

            master = value;

            if (master != null)
                BindToMaster();
        }
    }

    private Radio? master;
    [SerializeField] private StreamAudioClient audioClient = null!;
    [SerializeField] private AudioSourceController audioSourceController = null!;

    override public void Awake()
    {
        base.Awake();

        if (audioClient == null)
            throw new InvalidOperationException("AudioClient is null");

        if (audioSourceController == null)
            throw new InvalidOperationException("AudioSourceController is null");

        audioClient.ConvertToMono = true;
    }

    void OnEnable()
    {
        if (isGhost)
            return;

        if (Master != null)
            BindToMaster();
    }

    void OnDisable()
    {
        if (isGhost)
            return;

        if (Master != null)
            UnbindFromMaster();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (Master == null)
            {
                // Debug: find first small portable radio and set it as master
                var smallPortableRadios = FindObjectOfType<SmallPortableRadio>();

                if (smallPortableRadios != null)
                    Master = smallPortableRadios.GetComponent<Radio>();
            }
        }
    }

    private void BindToMaster()
    {
        if (master == null)
            throw new InvalidOperationException("Master is null");

        master.VolumeChanged += OnVolumeChanged;
        master.Toggled += OnToggled;
        master.RadioStationChanged += OnRadioStationChanged;

        OnVolumeChanged(master.Volume);
        OnToggled(master.IsOn);
        OnRadioStationChanged(master.RadioStation);
    }

    private void UnbindFromMaster()
    {
        if (master == null)
            throw new InvalidOperationException("Master is null");

        master.VolumeChanged -= OnVolumeChanged;
        master.Toggled -= OnToggled;
        master.RadioStationChanged -= OnRadioStationChanged;

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

    private void UpdateAudioClientBinding()
    {
        if (master == null || !master.IsOn || master.RadioStation == null)
        {
            audioClient.Host?.DetachClient(audioClient.gameObject);
        }
        else
        {
            StreamAudioHost? masterAudioHost = master.RadioStation == null ? null : AudioStreamManager.Instance.GetOrCreateHost(master.RadioStation);

            if (audioClient.Host != masterAudioHost)
            {
                audioClient.Host?.DetachClient(audioClient.gameObject);
                masterAudioHost?.AddClient(audioClient.gameObject);
            }
        }
    }
}
