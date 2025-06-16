using System;
using FishNet.Managing.Timing;
using FishNet.Object;
using GameKit.Utilities.Types;
using RealRadio.Components.Radio;
using RealRadio.Data;
using ScheduleOne.GameTime;
using UnityEngine;
using UnityEngine.UIElements;
using TimeManager = ScheduleOne.GameTime.TimeManager;

namespace RealRadio.Components.Building.Buildables;

public class SmallPortableRadio : Radio
{
    public override void Awake()
    {
        base.Awake();
    }

    public override void Start()
    {
        base.Start();

        if (isGhost)
            return;

        //volumeEditSlider.Value = Volume * volumeEditSlider.MaxValue;
    }

    protected override void OnPlayerUserChanged(NetworkObject prev, NetworkObject next, bool asServer)
    {
        base.OnPlayerUserChanged(prev, next, asServer);

        if (!asServer)
        {
            //stationEditSlider.gameObject.SetActive(next == Player.Local.NetworkObject);
            //volumeEditSlider.gameObject.SetActive(next == Player.Local.NetworkObject);
        }
    }
}

public class SmallPortableRadioScreenUI : MonoBehaviour
{
#nullable disable
    private SmallPortableRadio radio;
    private UIDocument document;

    private Label timeLabel;
    private Label stationNameLabel;
    private Label artistLabel;
    private Label titleLabel;
    private VisualElement logoContainer;
#nullable enable

    private void Awake()
    {
        radio = GetComponentInParent<SmallPortableRadio>() ?? throw new InvalidOperationException("No Radio component found in self or parents");
        document = GetComponent<UIDocument>() ?? throw new InvalidOperationException("No UIDocument component found on game object");

        radio.Toggled += OnRadioToggled;
    }

    private void Start()
    {
        OnRadioToggled(radio.IsOn);
    }

    private void OnEnable()
    {
        var root = document.rootVisualElement;
        timeLabel = root.Query<Label>(name: "Time").First() ?? throw new InvalidOperationException("Could not find time label ui element");
        stationNameLabel = root.Query<Label>(name: "StationName").First() ?? throw new InvalidOperationException("Could not find station name label ui element");
        artistLabel = root.Query<Label>(name: "SongArtist").First() ?? throw new InvalidOperationException("Could not find artist label ui element");
        titleLabel = root.Query<Label>(name: "SongTitle").First() ?? throw new InvalidOperationException("Could not find title label ui element");
        logoContainer = root.Query(name: "Logo").First() ?? throw new InvalidOperationException("Could not find logo container ui element");

        radio.VolumeChanged += OnVolumeChanged;
        radio.RadioStationChanged += OnRadioStationChanged;
        TimeManager.Instance.onMinutePass += OnMinutePass;

        OnVolumeChanged(radio.Volume);
        OnRadioStationChanged(radio.RadioStation);
    }

    private void OnDisable()
    {
        TimeManager.Instance.onMinutePass -= OnMinutePass;
        radio.VolumeChanged -= OnVolumeChanged;
        radio.RadioStationChanged -= OnRadioStationChanged;
        TimeManager.Instance.onMinutePass -= OnMinutePass;
    }

    private void OnDestroy()
    {
        radio.Toggled -= OnRadioToggled;
    }

    private void OnRadioToggled(bool isOn)
    {
        document.rootVisualElement.style.display = isOn ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnVolumeChanged(float volume)
    {
    }

    private void OnRadioStationChanged(RadioStation? station)
    {
        if (station == null)
        {
            stationNameLabel.text = "<i>No Station Selected</i>";
            artistLabel.text = "";
            titleLabel.text = "";
            logoContainer.style.display = DisplayStyle.None;
        }
        else
        {
            stationNameLabel.text = station.Name.EscapeRichText();
            logoContainer.style.display = DisplayStyle.Flex;
            UpdateSongInfo(station);

            // todo: update station icon
        }
    }

    private void OnMinutePass()
    {
        if (!radio.IsOn)
            return;

        int currentTime = TimeManager.Instance.CurrentTime;
        timeLabel.text = TimeManager.Get12HourTime(currentTime);

        UpdateSongInfo(radio.RadioStation);
    }

    private void UpdateSongInfo(RadioStation? station)
    {
        if (station == null)
        {
            artistLabel.text = "";
            titleLabel.text = "";

            return;
        }

        var songInfo = RadioStationInfoManager.Instance.GetSong(station);

        if (songInfo == null)
        {
            artistLabel.text = "<i>Song info unavailable</i>";
            titleLabel.text = "";
        }
        else
        {
            artistLabel.text = songInfo.Artist.EscapeRichText();
            titleLabel.text = songInfo.Title.EscapeRichText();
        }
    }
}
