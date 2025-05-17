using System;
using System.Buffers.Text;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Websocket.Client;

namespace SongInfoFetcher.OneFM;

public class OneFMSongInfoFetcher : WSSongInfoFetcher
{
    private readonly Uri uri;

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
    }

    public override async Task Start()
    {
        WSUri = await CreateWSUri();
        await base.Start();
    }

    private async Task<Uri> CreateWSUri()
    {
        var (sessionId, pingInterval) = await RequestSocketSessionInfo();

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
            new Uri($"https://socket.1.fm/socket.io/?EIO=3&transport=polling&t={DateTime.UtcNow.Ticks}-1")
        );
        string json = base64.Substring(5); // first 5 bytes are some kind of header or something, idk

        var response = JsonConvert.DeserializeObject<SocketIOResponse>(json) ?? throw new ArgumentException("Deserialized JSON is null");

        if (response.SessionId == null)
            throw new ArgumentException("Session ID is null");

        if (response.PingInterval == 0)
            throw new ArgumentException("Ping interval is 0");

        return (response.SessionId, response.PingInterval);
    }

    protected override Task<SongInfo> InternalRequestSongInfo()
    {
        throw new NotImplementedException();
    }

    protected override void OnMessageReceived(ResponseMessage message)
    {
        throw new NotImplementedException();
    }

    protected override void OnReconnected(ReconnectionInfo info)
    {
        throw new NotImplementedException();
    }
}
