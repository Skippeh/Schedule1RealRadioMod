using System;
using FishNet.Managing.Timing;
using FishNet.Object;
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
    }

    private void OnEnable()
    {
        var root = document.rootVisualElement;
        timeLabel = root.Query<Label>(name: "Time").First() ?? throw new InvalidOperationException("Could not find time label ui element");
        stationNameLabel = root.Query<Label>(name: "StationName").First() ?? throw new InvalidOperationException("Could not find station name label ui element");
        artistLabel = root.Query<Label>(name: "SongArtist").First() ?? throw new InvalidOperationException("Could not find artist label ui element");
        titleLabel = root.Query<Label>(name: "SongTitle").First() ?? throw new InvalidOperationException("Could not find title label ui element");
        logoContainer = root.Query(name: "Logo").First() ?? throw new InvalidOperationException("Could not find logo container ui element");

        TimeManager.Instance.onMinutePass += OnMinutePass;
    }

    private void OnDisable()
    {
        TimeManager.Instance.onMinutePass -= OnMinutePass;
    }

    private void OnMinutePass()
    {
        int currentTime = TimeManager.Instance.CurrentTime;
        timeLabel.text = TimeManager.Get12HourTime(currentTime, appendDesignator: true);
    }
}
