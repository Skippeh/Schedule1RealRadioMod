using System;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Websocket.Client;

namespace SongInfoFetcher.SimulatorRadio;

public class SimulatorRadioSongInfoFetcher : WSSongInfoFetcher
{
    public static Regex UriRegex { get; private set; } = new Regex(@"^(https?\://stream.simulatorradio.com(.*))|(https?\://simulatorradio.stream(.*))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override bool CanListenForSongInfo => true;

    public override bool CanRequestSongInfo => true;

    public SimulatorRadioSongInfoFetcher() : base(clientFactory: CreateClient, reconnectTimeout: TimeSpan.FromSeconds(15))
    {
        WSUri = new Uri("wss://ws.simulatorradio.com");
    }

    private static ClientWebSocket CreateClient()
    {
        var client = new ClientWebSocket();
        client.Options.SetRequestHeader("Origin", "https://simulatorradio.com");
        return client;
    }

    protected override async Task<SongInfo> InternalRequestSongInfo()
    {
        if (CurrentSong != null)
            return CurrentSong;

        return await Task.Run(async () =>
        {
            while (CurrentSong == null)
                await Task.Delay(100);

            return CurrentSong;
        });
    }

    protected override void OnDisconnected(DisconnectionInfo info)
    {
    }

    protected override void OnMessageReceived(ResponseMessage message)
    {
        if (message.MessageType != WebSocketMessageType.Text)
            return;

        string json = message.Text!;
        Message messageData;

        try
        {
            messageData = JsonConvert.DeserializeObject<Message>(json)!;
        }
        catch
        {
            return;
        }

        if (messageData.Type != MessageType.NowPlaying || messageData.NowPlaying == null)
            return;

        var nowPlaying = messageData.NowPlaying;

        if (nowPlaying.Title == null)
            return;

        SongInfo songInfo = new(nowPlaying.Title, nowPlaying.Artists);

        if (songInfo == CurrentSong)
            return;

        CurrentSong = songInfo;
        SongInfoReceived?.Invoke(CurrentSong);
    }

    protected override void OnReconnected(ReconnectionInfo info)
    {
    }
}
