using System;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using HashUtility;
using RealRadio.Components.Radio;
using RealRadio.Data;
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

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
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
#pragma warning restore CS0649

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

        btnOk.Click += OnClickOk;
        btnLeft.Click += OnClickLeft;
        btnRight.Click += OnClickRight;
        btnPower.Click += OnClickPower;
        btnVolume.Click += OnClickVolume;
        btnFavorite.Click += OnClickFavorite;
        btnFavorite1.Click += () => OnClickFavoriteIndexButton(0);
        btnFavorite2.Click += () => OnClickFavoriteIndexButton(1);
        btnFavorite3.Click += () => OnClickFavoriteIndexButton(2);
        btnFavorite4.Click += () => OnClickFavoriteIndexButton(3);

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
        if (!IsOn)
            return;

        switch (State)
        {
            case UiState.EditVolume:
            case UiState.SetFavorite:
                State = UiState.Default;
                break;
        }
    }

    private void OnClickLeft()
    {
        if (!IsOn)
            return;

        switch (State)
        {
            case UiState.EditVolume:
                SetVolume(Mathf.RoundToMultipleOf(Volume - 0.1f, 0.1f));
                break;
            case UiState.Default:
                ChangeStationIndex(-1);
                break;
        }
    }

    private void OnClickRight()
    {
        if (!IsOn)
            return;

        switch (State)
        {
            case UiState.EditVolume:
                SetVolume(Mathf.RoundToMultipleOf(Volume + 0.1f, 0.1f));
                break;
            case UiState.Default:
                ChangeStationIndex(1);
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
        if (!IsOn)
            return;

        if (State == UiState.EditVolume)
            State = UiState.Default;
        else
            State = UiState.EditVolume;
    }

    private void OnClickFavorite()
    {
        if (!IsOn)
            return;

        if (State == UiState.SetFavorite)
            State = UiState.Default;
        else
            State = UiState.SetFavorite;
    }

    private void OnClickFavoriteIndexButton(byte index)
    {
        if (!IsOn)
            return;

        switch (State)
        {
            case UiState.SetFavorite:
                SetFavoriteStation(index, RadioStation);
                State = UiState.Default;
                break;
            case UiState.Default:
                if (index >= FavoriteStations.Length)
                    return;

                var station = FavoriteStations[index];

                if (station == null)
                    return;

                SetRadioStationIdHash(station.Id?.GetStableHashCode());
                break;
        }
    }

    private void OnStateChanged(UiState prev, UiState next, bool asServer)
    {
        if (asServer)
            return;

        StateChanged?.Invoke(next);
    }

    private void ChangeStationIndex(int direction)
    {
        int currentIndex = RadioStation == null ? -1 : RadioStationManager.Instance.SortedStations.IndexOf(RadioStation);
        RadioStation? nextStation;

        if (currentIndex == -1)
            nextStation = direction == -1 ? RadioStationManager.Instance.SortedStations.LastOrDefault() : RadioStationManager.Instance.SortedStations.FirstOrDefault();
        else
        {
            int nextIndex = (currentIndex + direction) % RadioStationManager.Instance.SortedStations.Count;

            if (nextIndex < 0)
                nextIndex += RadioStationManager.Instance.SortedStations.Count;

            nextStation = RadioStationManager.Instance.SortedStations[nextIndex];
        }

        SetRadioStationIdHash(nextStation?.Id?.GetStableHashCode());
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
    private VisualElement volumeContainer;
    private VisualElement favoriteContainer;
    private Label favoriteIndexLabel;
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
        volumeContainer = root.Query(name: "Volume").First() ?? throw new InvalidOperationException("Could not find volume container ui element");
        favoriteContainer = root.Query(name: "Favorite").First() ?? throw new InvalidOperationException("Could not find favorite container ui element");
        favoriteIndexLabel = favoriteContainer.Query<Label>(name: "FavoriteIndex").First() ?? throw new InvalidOperationException("Could not find favorite index label ui element");

        radio.VolumeChanged += OnVolumeChanged;
        radio.RadioStationChanged += OnRadioStationChanged;
        radio.StateChanged += OnStateChanged;
        radio.FavoriteStationsChanged += OnFavoriteStationsChanged;
        TimeManager.Instance.onMinutePass += OnMinutePass;

        OnVolumeChanged(radio.Volume);
        OnRadioStationChanged(radio.RadioStation);
        OnStateChanged(radio.State);
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
        string className = volume >= 0.75f ? "high" : volume >= 0.5f ? "medium" : volume > 0f ? "low" : "muted";
        volumeContainer.RemoveFromClassList("high");
        volumeContainer.RemoveFromClassList("medium");
        volumeContainer.RemoveFromClassList("low");
        volumeContainer.RemoveFromClassList("muted");
        volumeContainer.AddToClassList(className);
    }

    private void OnRadioStationChanged(RadioStation? station)
    {
        if (station == null)
        {
            stationNameLabel.text = "<i>No Station Selected</i>";
            artistLabel.text = "";
            titleLabel.text = "";
        }
        else
        {
            stationNameLabel.text = station.Name.EscapeRichText();
            UpdateSongInfo(station);
        }

        UpdateFavoriteUI();
    }

    private void OnFavoriteStationsChanged(RadioStation?[] stations)
    {
        UpdateFavoriteUI();
    }

    private void UpdateFavoriteUI()
    {
        int index = radio.RadioStation == null ? -1 : Array.IndexOf(radio.FavoriteStations, radio.RadioStation);
        favoriteIndexLabel.text = index == -1 ? string.Empty : (index + 1).ToString();
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
        if (state == SmallPortableRadio.UiState.EditVolume)
            volumeContainer.AddToClassList("editing");
        else
            volumeContainer.RemoveFromClassList("editing");

        if (state == SmallPortableRadio.UiState.SetFavorite)
            favoriteContainer.AddToClassList("editing");
        else
            favoriteContainer.RemoveFromClassList("editing");
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
