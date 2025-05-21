using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using RealRadio.Components.Radio;
using ScheduleOne.Audio;
using ScheduleOne.Doors;
using ScheduleOne.GameTime;
using ScheduleOne.Map;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.Schedules;
using UnityEngine;

namespace RealRadio.Components.Buildings;

public class BuildingRadioProxy : RadioProxy
{
    public NPCEnterableBuilding? Building { get; set; }

    public int StartTime { get; private set; }
    public int StopTime { get; private set; }

    private bool startedOnceToday;

    protected override void Awake()
    {
        base.Awake();

        ScheduleOne.GameTime.TimeManager.Instance.onMinutePass += OnMinutePass;
        ScheduleOne.GameTime.TimeManager.Instance.onDayPass += OnDayPass;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        OnDayPass();
    }

    private void OnMinutePass()
    {
        if (IsClientOnly)
            return;

        if (ScheduleOne.GameTime.TimeManager.Instance.DailyMinTotal >= StopTime && RadioStationIndex != -1)
        {
            SetRadioStationIndex(-1);
        }

        if (!startedOnceToday && ScheduleOne.GameTime.TimeManager.Instance.DailyMinTotal >= StartTime && ScheduleOne.GameTime.TimeManager.Instance.DailyMinTotal < StopTime && RadioStationIndex == -1)
        {
            startedOnceToday = true;

            if (Building?.OccupantCount > 0 && UnityEngine.Random.Range(0f, 1f) <= 0.5f)
                SetRadioStationIndex(RadioStationManager.Instance.GetRandomNPCStationIndex());
        }
    }


    private void OnDayPass()
    {
        if (IsClientOnly)
            return;

        StopTime = 1440 - UnityEngine.Random.Range(1, 301);
        StartTime = UnityEngine.Random.Range(30, StopTime - 120);
        startedOnceToday = false;
    }

    protected override void InitAudioClient(bool delayStart = true)
    {
        if (IsClientOnly && Building == null)
        {
            StartCoroutine(WaitForReceiveBuildingThenInitAudioClient());
            return;
        }

        base.InitAudioClient(delayStart);

        audioClient!.ConvertToMono = true;
        audioClientObject!.SetActive(true);
    }

    private IEnumerator WaitForReceiveBuildingThenInitAudioClient()
    {
        yield return new WaitUntil(() => Building != null);
        InitAudioClient();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        RequestBuildingInfo();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBuildingInfo(NetworkConnection conn = null!)
    {
        if (Building == null)
            throw new InvalidOperationException("Building is null");

        ReceiveBuildingInfo(conn, BuildingRadioManager.GetBuildingHash(Building));
    }

    [TargetRpc]
    private void ReceiveBuildingInfo(NetworkConnection conn, uint buildingHash)
    {
        if (Building != null && IsClientOnly)
        {
            Plugin.Logger.LogWarning($"Received building again");
            return;
        }

        if (!BuildingRadioManager.Instance.Buildings.TryGetValue(buildingHash, out var building))
        {
            Plugin.Logger.LogError($"Building not found: {buildingHash}");
            return;
        }

        Building = building;
        OnBuildingSet();
    }

    private void OnBuildingSet()
    {
        if (Building == null)
            throw new InvalidOperationException("Building is null");

        name = $"{nameof(BuildingRadioProxy)} - {Building.name}";

        BuildingRadioManager.Instance.AddProxy(this);

        var parent = Building.GetComponentInChildren<StaticDoor>(includeInactive: true)?.gameObject;

        if (parent == null)
            parent = Building.gameObject;

        audioClientObject = Instantiate(AudioClientPrefab);
        audioClientObject.transform.SetParent(parent.transform, worldPositionStays: false);
        audioClientObject.SetActive(false);

        // Disable existing ambient loops in building
        var ambientSounds = Building
            .GetComponentsInChildren<AmbientLoop>(includeInactive: true)
            .Select(x => (MonoBehaviour)x)
            .Concat(Building.GetComponentsInChildren<AmbientLoopJukebox>(includeInactive: true));

        foreach (var ambientSound in ambientSounds)
        {
            ambientSound.GetComponent<AudioSource>().mute = true;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        BuildingRadioManager.Instance?.RemoveProxy(this);
    }
}
