using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AsmResolver;
using HashUtility;
using RealRadio.Components.Building;
using RealRadio.Components.UI;
using RealRadio.Components.Vehicles;
using RealRadio.Data;
using RealRadio.Patches;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vehicles;
using SongInfoFetcher;
using UnityEngine;

namespace RealRadio.Components.Radio;

public class VehicleRadioManager : NetworkSingleton<VehicleRadioManager>
{
    public Sprite TurnOffIcon = null!;

    private bool radialMenuOpen;

    private InteractableOption[]? radialMenuOptions;

    public override void Awake()
    {
        base.Awake();

        if (TurnOffIcon == null)
            throw new InvalidOperationException("TurnOffIcon is null");

        // Call OnVehicleSpawned for pre-existing vehicles
        foreach (var vehicle in FindObjectsOfType<LandVehicle>())
        {
            StartCoroutine(SpawnProxyAfterNetworkInit(vehicle));
        }

        LandVehicleStartPatch.OnVehicleSpawned += OnVehicleSpawned;
        RadioStationManager.Instance.OnStationsChanged += OnStationsChanged;
        RadioStationInfoManager.Instance.SongInfoUpdated += OnSongInfoUpdated;
    }

    private void OnVehicleSpawned(LandVehicle vehicle)
    {
        StartCoroutine(SpawnProxyAfterNetworkInit(vehicle));
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        LandVehicleStartPatch.OnVehicleSpawned -= OnVehicleSpawned;
        RadioStationManager.Instance.OnStationsChanged -= OnStationsChanged;
        RadioStationInfoManager.Instance.SongInfoUpdated -= OnSongInfoUpdated;
    }

    public override void Start()
    {
        radialMenuOptions = CreateRadialMenuOptions();
    }

    private void OnStationsChanged()
    {
        radialMenuOptions = CreateRadialMenuOptions();
    }

    private void OnSongInfoUpdated(RadioStation station, SongInfo info)
    {
        if (radialMenuOptions == null)
            return;

        int indexOfStation = RadioStationManager.Instance.SortedStations.IndexOf(station);

        if (indexOfStation == -1)
            return;

        radialMenuOptions[indexOfStation + 1].Description = info.ToString().EscapeRichText();
    }

    private InteractableOption[] CreateRadialMenuOptions()
    {
        var options = new InteractableOption[RadioStationManager.Instance.SortedStations.Count + 1];

        options[0] = InteractableOption.CreateOption(
            id: string.Empty,
            name: "Turn off",
            sprite: TurnOffIcon
        );

        for (int i = 0; i < RadioStationManager.Instance.SortedStations.Count; ++i)
        {
            var station = RadioStationManager.Instance.SortedStations[i];
            string? songInfo = RadioStationInfoManager.Instance.GetSong(station)?.ToString().EscapeRichText();

            if (songInfo == null)
            {
                songInfo = "<i>Song info unavailable</i>";
            }

            options[i + 1] = InteractableOption.CreateOption(
                id: station.Id!,
                name: station.Name!.EscapeRichText(),
                description: songInfo,
                sprite: station.Icon,
                abbreviation: station.Abbreviation,
                backgroundColor: station.BackgroundColor,
                textColor: station.TextColor,
                roundedBackground: station.RoundedBackground
            );
        }

        return options;
    }

    private IEnumerator SpawnProxyAfterNetworkInit(LandVehicle vehicle)
    {
        if (!IsServer && !IsClient)
            yield return new WaitUntil(() => IsServer || IsClient);

        if (!IsServer)
            yield break;

        var proxyPrefab = Plugin.Assets!.Prefabs.VehicleRadioProxy;
        var proxy = Instantiate(proxyPrefab, parent: transform);
        proxy.GetComponent<VehicleRadioProxy>().Vehicle = vehicle;
        Spawn(proxy);
    }

    private void Update()
    {
        UpdateRadialMenu();
    }

    private void UpdateRadialMenu()
    {
        if (GameInput.GetButtonUp(GameInput.ButtonCode.Reload) || GameInput.IsTyping || Player.Local?.CurrentVehicle == null)
        {
            if (radialMenuOpen)
            {
                radialMenuOpen = false;
                RadialMenu.Instance.Hide();
            }

            return;
        }

        if (!GameInput.GetButtonDown(GameInput.ButtonCode.Reload) || GameInput.IsTyping)
            return;

        // Only show the radial menu if the current vehicle has a proxy
        var proxyRef = Player.Local.CurrentVehicle.GetComponent<VehicleRadioProxyReference>();

        if (proxyRef == null)
            return;

        if (radialMenuOpen)
            return;

        if (radialMenuOptions == null)
        {
            Plugin.Logger.LogWarning("Radio station options are null");
            return;
        }

        var selectedOption = radialMenuOptions.FirstOrDefault(x => x.Id == proxyRef.Proxy?.RadioStation?.Id) ?? radialMenuOptions[0];

        radialMenuOpen = true;
        RadialMenu.Instance.Show(radialMenuOptions, onOptionSelected: OnRadialOptionSelected, selectedOption: selectedOption);
    }

    private void OnRadialOptionSelected(InteractableOption option)
    {
        var proxyRef = Player.Local.CurrentVehicle.GetComponent<VehicleRadioProxyReference>();
        proxyRef?.Proxy?.SetRadioStationIdHash(option.Id == string.Empty ? null : option.Id?.GetStableHashCode());

        radialMenuOpen = false;
        RadialMenu.Instance.Hide();
    }
}
