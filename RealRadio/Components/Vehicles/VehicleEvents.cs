using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace RealRadio.Components.Vehicles;

public class VehicleEvents : Singleton<VehicleEvents>
{
    public delegate void PlayerEnterVehicleDelegate(Player player, LandVehicle vehicle);
    public delegate void PlayerExitVehicleDelegate(Player player, LandVehicle vehicle, Transform exitPoint);

    /// <summary>
    /// Called when a player enters a vehicle.
    /// Note: This is called for every player that is currently in a vehicle when this singleton is created.
    /// </summary>
    public PlayerEnterVehicleDelegate? PlayerEnterVehicle { get; set; }

    /// <summary>
    /// Called when a player exits a vehicle.
    /// </summary>
    public PlayerExitVehicleDelegate? PlayerExitVehicle { get; set; }

    private void OnEnable()
    {
        Player.onPlayerSpawned += OnPlayerSpawned;

        foreach (Player player in Player.PlayerList)
        {
            if (player.CurrentVehicle != null)
            {
                OnPlayerEnterVehicle(player, player.CurrentVehicle.GetComponent<LandVehicle>());
            }
        }
    }

    private void OnDisable()
    {
        Player.onPlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerSpawned(Player player)
    {
        player.onEnterVehicle += (vehicle) => OnPlayerEnterVehicle(player, vehicle);
        player.onExitVehicle += (vehicle, exitPoint) => OnPlayerExitVehicle(player, vehicle, exitPoint);
    }

    private void OnPlayerEnterVehicle(Player player, LandVehicle vehicle)
    {
        PlayerEnterVehicle?.Invoke(player, vehicle);
    }

    private void OnPlayerExitVehicle(Player player, LandVehicle vehicle, Transform exitPoint)
    {
        PlayerExitVehicle?.Invoke(player, vehicle, exitPoint);
    }
}
