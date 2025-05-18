using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Websocket.Client;

namespace SongInfoFetcher.GlobalPlayer;

public class GlobalPlayerSongFetcher : WSSongInfoFetcher
{
    internal static Regex UriRegex = new Regex(@"https?://media-ice.musicradio.com/(?<station>[^_/?]+)MP3.*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override bool CanListenForSongInfo => true;

    public override bool CanRequestSongInfo => true;

    private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
    private int heraldId;

    public GlobalPlayerSongFetcher(Uri uri) : base(clientFactory: CreateClient, reconnectTimeout: TimeSpan.FromSeconds(5))
    {
        WSUri = new Uri("wss://metadata.musicradio.com/v2/now-playing");
    }

    private static ClientWebSocket CreateClient()
    {
        var client = new ClientWebSocket();
        client.Options.SetRequestHeader("Origin", "https://www.globalplayer.com");
        return client;
    }

    public override async Task Start()
    {
        var metaData = await MetaDataFetcher.FetchMetaData();
        Console.WriteLine($"Got meta data: {metaData}");

        await base.Start();
    }

    protected override async Task<SongInfo> InternalRequestSongInfo()
    {
        if (CurrentSong != null)
            return CurrentSong;

        while (CurrentSong == null)
            await Task.Delay(100);

        return CurrentSong;
    }

    protected override void OnDisconnected(DisconnectionInfo info)
    {
    }

    protected override void OnMessageReceived(ResponseMessage message)
    {
        Console.WriteLine($"Message received: {message.Text}");
    }

    protected override void OnReconnected(ReconnectionInfo info)
    {
        SendAction(new SubscribeRequest(heraldId));
    }

    private void SendAction(RequestAction action)
    {
        SendActions([action]);
    }

    private void SendActions(IEnumerable<RequestAction> actions)
    {
        Send(SerializeJson(new ActionsRequest(actions.ToList())));
    }

    private string SerializeJson(object obj)
    {
        return JsonConvert.SerializeObject(obj, JsonSerializerSettings);
    }
}
