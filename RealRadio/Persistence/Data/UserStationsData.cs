using System.Collections.Generic;
using Newtonsoft.Json;
using RealRadio.Components.API.Data;
using ScheduleOne.Persistence.Datas;

namespace RealRadio.Peristence.Data;

public class UserStationsData : SaveData
{
    public List<RadioStation> Stations;

    public UserStationsData(List<RadioStation> stations)
    {
        Stations = stations;
    }

    public override string GetJson(bool prettyPrint = true)
    {
        return JsonConvert.SerializeObject(this, prettyPrint ? Formatting.Indented : Formatting.None);
    }
}
