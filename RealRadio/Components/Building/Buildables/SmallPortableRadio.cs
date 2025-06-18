using System;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
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
    public event Action<UiState>? StateChanged;

    [field: SyncVar(Channel = Channel.Reliable, ReadPermissions = ReadPermission.Observers, WritePermissions = WritePermission.ServerOnly, OnChange = nameof(OnStateChanged))]
    public UiState State { get; [ServerRpc(RequireOwnership = false, RunLocally = true)] set; }

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

        btnOk.CursorDown += OnClickOk;
        btnLeft.CursorDown += OnClickLeft;
        btnRight.CursorDown += OnClickRight;
        btnPower.CursorDown += OnClickPower;
        btnVolume.CursorDown += OnClickVolume;
        btnFavorite.CursorDown += OnClickFavorite;
        btnFavorite1.CursorDown += OnClickFavorite1;
        btnFavorite2.CursorDown += OnClickFavorite2;
        btnFavorite3.CursorDown += OnClickFavorite3;
        btnFavorite4.CursorDown += OnClickFavorite4;

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

    private void OnClickOk()
    {
        switch (State)
        {
            case UiState.EditVolume:
                State = UiState.Default;
                break;
        }
    }

    private void OnClickLeft()
    {
        switch (State)
        {
            case UiState.EditVolume:
                SetVolume(Volume - 0.1f);
                break;
            case UiState.Default:
                Plugin.Logger.LogInfo("Switch to previous station");
                // todo: change to previous station
                break;
        }
    }

    private void OnClickRight()
    {
        switch (State)
        {
            case UiState.EditVolume:
                SetVolume(Volume + 0.1f);
                break;
            case UiState.Default:
                Plugin.Logger.LogInfo("Switch to next station");
                // todo: change to next station
                break;
        }
    }

    private void OnClickPower()
    {
        IsOn = !IsOn;
        State = UiState.Default;
    }

    private void OnClickVolume()
    {
        if (State == UiState.EditVolume)
            State = UiState.Default;
        else
            State = UiState.EditVolume;
    }

    private void OnClickFavorite()
    {
        if (State == UiState.SetFavorite)
            State = UiState.Default;
        else
            State = UiState.SetFavorite;
    }

    private void OnClickFavorite1()
    {
        if (State != UiState.SetFavorite)
            return;

        SetFavoriteStation(0, RadioStation);
        State = UiState.Default;
    }

    private void OnClickFavorite2()
    {
        if (State != UiState.SetFavorite)
            return;

        SetFavoriteStation(1, RadioStation);
        State = UiState.Default;
    }

    private void OnClickFavorite3()
    {
        if (State != UiState.SetFavorite)
            return;

        SetFavoriteStation(2, RadioStation);
        State = UiState.Default;
    }

    private void OnClickFavorite4()
    {
        if (State != UiState.SetFavorite)
            return;

        SetFavoriteStation(3, RadioStation);
        State = UiState.Default;
    }

    private void OnStateChanged(UiState prev, UiState next, bool asServer)
    {
        if (asServer)
            return;

        StateChanged?.Invoke(next);
    }

    private void SetFavoriteStation(int index, RadioStation? radioStation)
    {
        if (radioStation == null)
            return;

        // todo: set favorite
        Plugin.Logger.LogInfo($"Set favorite in slot {index} to {radioStation}");
    }

    public enum UiState
    {
        Default,
        EditVolume,
        SetFavorite,
    }
}

public class SmallPortableRadioScreenUI : MonoBehaviour
{
#nullable disable
    private SmallPortableRadio radio;
    private UIDocument document;

    private VisualElement rootContainer;
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
        rootContainer = root.Query(name: "Root").First() ?? throw new InvalidOperationException("Could not find root ui element");
        timeLabel = root.Query<Label>(name: "Time").First() ?? throw new InvalidOperationException("Could not find time label ui element");
        stationNameLabel = root.Query<Label>(name: "StationName").First() ?? throw new InvalidOperationException("Could not find station name label ui element");
        artistLabel = root.Query<Label>(name: "SongArtist").First() ?? throw new InvalidOperationException("Could not find artist label ui element");
        titleLabel = root.Query<Label>(name: "SongTitle").First() ?? throw new InvalidOperationException("Could not find title label ui element");
        logoContainer = root.Query(name: "Logo").First() ?? throw new InvalidOperationException("Could not find logo container ui element");

        radio.VolumeChanged += OnVolumeChanged;
        radio.RadioStationChanged += OnRadioStationChanged;
        radio.StateChanged += OnStateChanged;
        TimeManager.Instance.onMinutePass += OnMinutePass;

        OnVolumeChanged(radio.Volume);
        OnRadioStationChanged(radio.RadioStation);
    }

    private void OnDisable()
    {
        TimeManager.Instance.onMinutePass -= OnMinutePass;
        radio.VolumeChanged -= OnVolumeChanged;
        radio.RadioStationChanged -= OnRadioStationChanged;
        radio.StateChanged -= OnStateChanged;
        TimeManager.Instance.onMinutePass -= OnMinutePass;
    }

    private void OnDestroy()
    {
        radio.Toggled -= OnRadioToggled;
    }

    private void OnRadioToggled(bool isOn)
    {
        if (!isOn)
        {
            rootContainer.AddToClassList("hidden");
        }
        else
        {
            rootContainer.RemoveFromClassList("hidden");
            OnMinutePass();
        }
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

    private void OnStateChanged(SmallPortableRadio.UiState state)
    {
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
