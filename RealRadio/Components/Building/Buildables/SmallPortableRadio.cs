using System;
using FishNet.Managing.Timing;
using FishNet.Object;
using GameKit.Utilities.Types;
using RealRadio.Components.Radio;
using RealRadio.Data;
using ScheduleOne.GameTime;
using ScheduleOne.PlayerScripts;
using UnityEngine;
using UnityEngine.UIElements;
using TimeManager = ScheduleOne.GameTime.TimeManager;
using WorldButton = RealRadio.Components.WorldUI.Button;

namespace RealRadio.Components.Building.Buildables;

public class SmallPortableRadio : Radio
{
#nullable disable
    [Header("References")]
    [SerializeField] private WorldButton btnOk;
    [SerializeField] private WorldButton btnLeft;
    [SerializeField] private WorldButton btnRight;
    [SerializeField] private WorldButton btnPower;
    [SerializeField] private WorldButton btnVolume;
    [SerializeField] private WorldButton btnFavorite;
    [SerializeField] private WorldButton btnFavorite1;
    [SerializeField] private WorldButton btnFavorite2;
    [SerializeField] private WorldButton btnFavorite3;
    [SerializeField] private WorldButton btnFavorite4;
#nullable enable

    public override void Awake()
    {
        base.Awake();

        if (btnOk == null)
            throw new InvalidOperationException("btnOk is null");

        if (btnLeft == null)
            throw new InvalidOperationException("btnLeft is null");

        if (btnRight == null)
            throw new InvalidOperationException("btnRight is null");

        if (btnPower == null)
            throw new InvalidOperationException("btnPower is null");

        if (btnVolume == null)
            throw new InvalidOperationException("btnVolume is null");

        if (btnFavorite == null)
            throw new InvalidOperationException("btnFavorite is null");

        if (btnFavorite1 == null)
            throw new InvalidOperationException("btnFavorite1 is null");

        if (btnFavorite2 == null)
            throw new InvalidOperationException("btnFavorite2 is null");

        if (btnFavorite3 == null)
            throw new InvalidOperationException("btnFavorite3 is null");

        if (btnFavorite4 == null)
            throw new InvalidOperationException("btnFavorite4 is null");

        ToggleInputState(enabled: false);
    }

    private void ToggleInputState(bool enabled)
    {
        btnOk.enabled = enabled;
        btnLeft.enabled = enabled;
        btnRight.enabled = enabled;
        btnPower.enabled = enabled;
        btnVolume.enabled = enabled;
        btnFavorite.enabled = enabled;
        btnFavorite1.enabled = enabled;
        btnFavorite2.enabled = enabled;
        btnFavorite3.enabled = enabled;
        btnFavorite4.enabled = enabled;
    }

    protected override void OnPlayerUserChanged(NetworkObject prev, NetworkObject next, bool asServer)
    {
        base.OnPlayerUserChanged(prev, next, asServer);

        if (!asServer)
        {
            ToggleInputState(enabled: next == Player.Local.NetworkObject);
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
