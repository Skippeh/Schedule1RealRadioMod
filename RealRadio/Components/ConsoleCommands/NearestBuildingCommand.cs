using System.Collections.Generic;
using System.Linq;
using System.Text;
using RealRadio.Components.Buildings;
using RealRadio.Components.Radio;
using ScheduleOne;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace RealRadio.Compnoents.ConsoleCommands;

public class NearestBuildingCommand : Console.ConsoleCommand
{
    public override string CommandWord => "rr_nearestbuilding";

    public override string CommandDescription => "Print the name of the nearest residential building";

    public override string ExampleUsage => "rr_nearestbuilding";

    public override void Execute(List<string> args)
    {
        var plrPosition = Player.Local.transform.position;
        var building = BuildingRadioManager.Instance.Buildings.Values.OrderBy(b => (plrPosition - b.transform.position).sqrMagnitude).FirstOrDefault();
        var builder = new StringBuilder();

        if (building == null)
        {
            builder.Append("No building found");
        }
        else
        {
            builder.AppendLine($"Nearest building: {building.BuildingName} (unity name: {building.name})");

            double distance = (plrPosition - building.transform.position).magnitude;
            builder.AppendLine($"Distance from player: {distance}m");

            BuildingRadioProxy proxy = BuildingRadioManager.Instance.Proxies[building];
            Data.RadioStation? radioStation = proxy.RadioStation;
            builder.AppendLine($"Current station: {radioStation?.Name ?? "None"}");

            if (radioStation != null)
            {
                builder.AppendLine($"Current song: {RadioStationInfoManager.Instance.GetSong(radioStation)?.ToString() ?? "Song info unavailable"}");
            }
        }

        Console.Log(builder.ToString());
    }
}
