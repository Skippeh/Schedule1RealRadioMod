using System;
using ScheduleOne.Audio;
using ScheduleOne.Vehicles;

namespace RealRadio;

public static class GameEvents
{
    public static Action<LandVehicle>? VehicleSpawned;
    public static Action<MusicTrack, bool>? MusicTrackToggled;
    public static Action<MusicTrack>? MusicTrackPlay;
}
