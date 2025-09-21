using System;
using System.Collections.Generic;
using System.Linq;
using HashUtility;
using RealRadio.Components.Radio;
using ScheduleOne.PlayerScripts;
using Console = ScheduleOne.Console;

namespace RealRadio.Components.ConsoleCommands;

public class SetNearestBuildingStationCommand : Console.ConsoleCommand
{
    public override string CommandWord => "rr_setnearestbuildingstation";

    public override string CommandDescription => "Set the nearest building's radio station. Optionally provide a station name, or 'none' to stop it.";

    public override string ExampleUsage => "rr_setnearestbuildingstation simulator radio";

    public override void Execute(List<string> args)
    {
        var plrPosition = Player.Local.transform.position;
        var building = BuildingRadioManager.Instance.Buildings.Values.OrderBy(b => (plrPosition - b.transform.position).sqrMagnitude).FirstOrDefault();

        if (building == null)
        {
            Console.Log("No building found");
            return;
        }

        var proxy = BuildingRadioManager.Instance.Proxies[building];
        string? stationName = args.Count > 0 ? string.Join(" ", args) : null;
        Data.RadioStation? station = null;

        if (stationName == null)
        {
            station = RadioStationManager.Instance.GetRandomNPCStation();
        }
        else if (!stationName.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var station2 in RadioStationManager.Instance.Stations)
            {
                if (station2.Name!.Contains(stationName, StringComparison.OrdinalIgnoreCase))
                {
                    station = station2;
                    break;
                }
            }
        }

        if (station == null && stationName?.Equals("none", StringComparison.OrdinalIgnoreCase) != true)
        {
            Console.Log("No station found");
            return;
        }

        proxy.SetRadioStationIdHash(station?.Id!.GetStableHashCode());
    }
}
