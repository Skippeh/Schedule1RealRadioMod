using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using RealRadio.Data;

namespace RealRadio.Components.API;

public class CustomRadioStations
{
    public CustomRadioStations(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
            Directory.CreateDirectory(rootDirectory);
    }

    public async Task<List<RadioStation>> LoadStationsFromDisk()
    {
        var result = new List<RadioStation>();
        return result;
    }
}
