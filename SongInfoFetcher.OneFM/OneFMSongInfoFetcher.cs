using System;
using System.Buffers.Text;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongInfoFetcher.OneFM.Data;
using Websocket.Client;

namespace SongInfoFetcher.OneFM;

public class OneFMSongInfoFetcher : WSSongInfoFetcher
{
    internal static readonly Regex UriRegex = new Regex(@"^https://strm\d+\.1\.fm/(?<station>[^_/?]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Uri uri;
    private readonly string radioStation;
    private int pingInterval;
    private CancellationTokenSource? pingCancellationTokenSource;

    private readonly WebClient webClient = new()
    {
        Headers =
        {
            { "Origin", "https://www.1.fm" }
        }
    };

    public override bool CanListenForSongInfo => true;
    public override bool CanRequestSongInfo => true;

    public OneFMSongInfoFetcher(Uri uri) : base()
    {
        this.uri = uri;
        radioStation = ParseRadioStation(uri);
    }

    private string ParseRadioStation(Uri uri)
    {
        var match = UriRegex.Match(uri.ToString());

        if (match.Success)
            return match.Groups["station"].Value;

        // Should not be reachable unless this fetcher was added manually by the user with a non conforming uri
        throw new ArgumentException("Invalid uri", nameof(uri));
    }

    public override async Task Start()
    {
        WSUri = await CreateWSUri();
        await base.Start();
    }

    private async Task<Uri> CreateWSUri()
    {
        var (sessionId, pingInterval) = await RequestSocketSessionInfo();
        this.pingInterval = (int)pingInterval;

        var builder = new UriBuilder(uri)
        {
            Scheme = "wss",
            Host = "socket.1.fm",
            Path = "/socket.io/",
            Query = $"EIO=3&transport=websocket&sid={HttpUtility.UrlEncode(sessionId)}"
        };

        return builder.Uri;
    }

    private async Task<(string, uint)> RequestSocketSessionInfo()
    {
        string base64 = await webClient.DownloadStringTaskAsync(
            new Uri($"https://socket.1.fm/socket.io/?EIO=3&transport=polling&t={DateTime.UtcNow.Ticks}")
        );
        string json = base64.Substring(5); // first 5 bytes are some kind of header or something, idk

        var response = JsonConvert.DeserializeObject<SocketIOResponse>(json) ?? throw new ArgumentException("Deserialized JSON is null");

        if (response.SessionId == null)
            throw new ArgumentException("Session ID is null");

        if (response.PingInterval == 0)
            throw new ArgumentException("Ping interval is 0");

        return (response.SessionId, response.PingInterval);
    }

    protected override async Task<SongInfo> InternalRequestSongInfo()
    {
        if (CurrentSong != null)
            return CurrentSong;

        string json = await webClient.DownloadStringTaskAsync($"https://www.1.fm/stplaylist/{HttpUtility.UrlEncode(radioStation)}");
        var newsData = JsonConvert.DeserializeObject<NewsData>(json) ?? throw new ArgumentException("Deserialized JSON is null");

        var currentSongData = newsData.NowPlaying.FirstOrDefault();
        SongInfo songInfo = new SongInfo(CapitalizeWords(currentSongData.Title)!, CapitalizeWords(currentSongData.Artist));
        CurrentSong = songInfo;
        SongInfoReceived?.Invoke(songInfo);
        return songInfo;
    }

    protected override void OnMessageReceived(ResponseMessage message)
    {
        if (message.MessageType != WebSocketMessageType.Text)
            return;

        HandleMessage(message.Text!);
    }

    private void HandleMessage(string messageText)
    {
        if (messageText == "3probe")
        {
            Send("5"); // upgrade
            SendEmit("room", radioStation); // join radio station room so we receive news when song changes
        }
        else if (messageText.StartsWith("4")) // event
        {
            messageText = messageText.Substring(1);

            string msgId;
            string? data;

            if (!messageText.Contains("[", StringComparison.InvariantCulture))
            {
                msgId = messageText;
                data = null;
            }
            else
            {
                msgId = messageText.Substring(0, messageText.IndexOf("[", StringComparison.InvariantCulture));
                data = messageText.Substring(msgId.Length);
            }

            if (!int.TryParse(msgId, out int messageId))
                return;

            if (messageId == 0)
                return;

            if (messageId == 2 && data != null)
                HandleEvent(data);
        }
    }

    private void HandleEvent(string data)
    {
        JArray jArray;

        try
        {
            jArray = JArray.Parse(data);
        }
        catch
        {
            return;
        }

        if (jArray.Count == 0)
            return;

        string eventName = jArray[0].ToString();
        var eventData = jArray[1];

        if (eventName == "news")
            HandleNewsEvent(eventData);
    }

    private void HandleNewsEvent(JToken eventData)
    {
        if (eventData.Type != JTokenType.Object)
            return;


        EventData<NewsData>? newsData;

        try
        {
            newsData = eventData.ToObject<EventData<NewsData>>();

            if (newsData == null)
                return;
        }
        catch
        {
            return;
        }

        var currentSong = newsData?.Data?.NowPlaying.FirstOrDefault();

        if (currentSong == null)
            return;

        CurrentSong = new SongInfo(CapitalizeWords(currentSong.Title)!, CapitalizeWords(currentSong.Artist));
        SongInfoReceived?.Invoke(CurrentSong);
    }

    protected override void OnReconnected(ReconnectionInfo info)
    {
        if (pingCancellationTokenSource != null)
            throw new InvalidOperationException("Ping loop is already running");

        HandShake();

        pingCancellationTokenSource = new();
        Task.Run(() => PingLoop(pingCancellationTokenSource.Token));
    }

    private void HandShake()
    {
        Client.Send("2probe");
    }

    private async Task PingLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(millisecondsDelay: pingInterval, cancellationToken);
            Send("2");
        }
    }

    protected override void OnDisconnected(DisconnectionInfo info)
    {
        if (pingCancellationTokenSource != null)
        {
            pingCancellationTokenSource.Cancel();
            pingCancellationTokenSource.Dispose();
            pingCancellationTokenSource = null;
        }

        CurrentSong = null;
    }

    private void SendEmit(string id, object data)
    {
        string[] message = [id, data as string ?? JsonConvert.SerializeObject(data)];
        Send($"420{JsonConvert.SerializeObject(message)}");
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

        static bool ShouldCapitalizeNextCharacter(char ch) => !char.IsLetter(ch) && ch != '\'';
    }
}
