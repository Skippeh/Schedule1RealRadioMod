using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using SongInfoFetcher.OneFM.Data;

namespace SongInfoFetcher.OneFM;

public class OneFMSongInfoFetcher : HttpRequestSongInfoFetcher
{
    internal static readonly Regex UriRegex = new Regex(@"^https://strm\d+\.1\.fm/(?<station>[^_/?]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string radioStation;
    private readonly Uri playlistUri;

    public OneFMSongInfoFetcher(Uri uri)
    {
        radioStation = ParseRadioStation(uri);
        playlistUri = new Uri($"https://playlist2.1.fm/channels/play_history/?channel={HttpUtility.UrlEncode(radioStation)}");

        WebClient.Headers.Add("Origin", "https://radio.1cloud.fm");
        WebClient.Headers.Add("Referer", "https://radio.1cloud.fm/");
    }

    public override async Task<SongInfo> RequestSongInfo()
    {
        string json = await WebClient.DownloadStringTaskAsync(playlistUri);
        HistorySong[] history = JsonConvert.DeserializeObject<HistorySong[]>(json) ?? throw new HttpRequestException("Deserialized JSON is null");
        var song = history.First();
        return CurrentSong = new SongInfo(song.Title, song.Artist);
    }

    private string ParseRadioStation(Uri uri)
    {
        var match = UriRegex.Match(uri.ToString());

        if (match.Success)
            return match.Groups["station"].Value;

        // Should not be reachable unless this fetcher was added manually by the user with a non conforming uri
        throw new ArgumentException("Invalid uri", nameof(uri));
    }
}
