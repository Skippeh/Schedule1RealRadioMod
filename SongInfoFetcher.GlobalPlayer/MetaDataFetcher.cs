using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Newtonsoft.Json;
using SongInfoFetcher.GlobalPlayer.Data;

namespace SongInfoFetcher.GlobalPlayer;

internal static class MetaDataFetcher
{
    private static BrowsingContext context = new(Configuration.Default.WithDefaultLoader());

    public static async Task<MetaData> FetchMetaData()
    {
        var document = await context.OpenAsync("https://www.globalplayer.com");
        var dataElm = document.GetElementById("__NEXT_DATA__");

        if (dataElm == null)
            throw new HttpRequestException("Could not find __NEXT_DATA__ element");

        string json = dataElm.Text();

        return JsonConvert.DeserializeObject<MetaData>(json, GlobalPlayerSongFetcher.JsonSerializerSettings) ?? throw new HttpRequestException("Deserialized JSON is null");
    }
}
