using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using SocketIOClient;
using SongInfoFetcher.OneFM.Data;

namespace SongInfoFetcher.OneFM;

public class OneFMSongInfoFetcher : SocketIOSongInfoFetcher
{
    internal static readonly Regex UriRegex = new Regex(@"^https://strm\d+\.1\.fm/(?<station>[^_/?]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override bool CanListenForSongInfo => true;
    public override bool CanRequestSongInfo => true;

    private readonly string radioStation;
    private readonly Uri playlistUri;

    public OneFMSongInfoFetcher(Uri uri) : base(new Uri("https://socket.1.fm"), GetSocketIOOptions())
    {
        radioStation = ParseRadioStation(uri);
        playlistUri = new Uri($"https://www.1.fm/stplaylist/{HttpUtility.UrlEncode(radioStation)}");
    }

    private string ParseRadioStation(Uri uri)
    {
        var match = UriRegex.Match(uri.ToString());

        if (match.Success)
            return match.Groups["station"].Value;

        // Should not be reachable unless this fetcher was added manually by the user with a non conforming uri
        throw new ArgumentException("Invalid uri", nameof(uri));
    }

    private static SocketIOOptions? GetSocketIOOptions()
    {
        return new SocketIOOptions
        {
            ExtraHeaders = new()
            {
                { "Origin", "https://1.fm" }
            },
            AutoUpgrade = true,
            EIO = SocketIO.Core.EngineIO.V3,
            Reconnection = true,
            ReconnectionDelay = 5000,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        };
    }

    public override Task Start()
    {
        Client.On("news", OnNewsReceived);
        Client.On("playernews", OnNewsReceived);
        return base.Start();
    }

    public override async Task<SongInfo> InternalRequestSongInfo()
    {
        if (CurrentSong != null)
            return CurrentSong;

        string json = await Client.HttpClient.GetStringAsync(playlistUri);
        var newsData = JsonConvert.DeserializeObject<NewsData>(json) ?? throw new ArgumentException("Deserialized JSON is null");

        var currentSongData = newsData.NowPlaying.FirstOrDefault();
        SongInfo songInfo = new SongInfo(CapitalizeWords(currentSongData.Title)!, CapitalizeWords(currentSongData.Artist));
        CurrentSong = songInfo;
        SongInfoReceived?.Invoke(songInfo);
        return songInfo;
    }

    protected override void OnConnected()
    {
        Client.EmitAsync("room", radioStation);
    }

    private void OnNewsReceived(SocketIOResponse response)
    {
        EventData<NewsData>? newsData;

        try
        {
            newsData = response.GetValue<EventData<NewsData>>();
        }
        catch
        {
            return;
        }

        if (newsData?.Data?.NowPlaying == null)
            return;

        var currentSongData = newsData.Data.NowPlaying.FirstOrDefault();
        CurrentSong = new SongInfo(CapitalizeWords(currentSongData.Title), CapitalizeWords(currentSongData.Artist));
        SongInfoReceived?.Invoke(CurrentSong);
    }

    private string? CapitalizeWords(string? input)
    {
        if (input == null)
            return null;

        var builder = new StringBuilder(input);
        for (int i = 0; i < builder.Length; i++)
        {
            if (i == 0 || ShouldCapitalizeNextCharacter(builder[i - 1]))
                builder[i] = char.ToUpper(builder[i]);
        }

        return builder.ToString();

        static bool ShouldCapitalizeNextCharacter(char ch) => !char.IsLetter(ch) && ch != '\'' && ch != '’';
    }
}
