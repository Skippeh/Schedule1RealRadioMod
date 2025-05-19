using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SongInfoFetcher.GlobalPlayer.Data;
using Websocket.Client;

namespace SongInfoFetcher.GlobalPlayer;

public class GlobalPlayerSongFetcher : WSSongInfoFetcher
{
    internal static Regex UriRegex = new Regex(@"https?://(media-ice\.musicradio\.com/(?<station>[^/?]+)MP3.*|(media-ssl\.musicradio\.com/(?<station>[^/?]+)).*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override bool CanListenForSongInfo => true;

    public override bool CanRequestSongInfo => true;

    internal static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
    private int heraldId;
    private string stationSlug;

    public GlobalPlayerSongFetcher(Uri uri) : base(clientFactory: CreateClient, reconnectTimeout: TimeSpan.FromSeconds(5))
    {
        WSUri = new Uri("wss://metadata.musicradio.com/v2/now-playing");
        stationSlug = ParseStationSlug(uri);
    }

    private static ClientWebSocket CreateClient()
    {
        var client = new ClientWebSocket();
        client.Options.SetRequestHeader("Origin", "https://www.globalplayer.com");
        return client;
    }

    private static string ParseStationSlug(Uri uri)
    {
        var match = UriRegex.Match(uri.ToString());

        if (!match.Success)
            throw new ArgumentException("Invalid uri", nameof(uri));

        return match.Groups["station"].Value;
    }

    public override async Task Start()
    {
        var metaData = await MetaDataFetcher.FetchMetaData();
        var brands = metaData.Props.PageProps.Feature.Blocks.FirstOrDefault(x => x.Type == BlockType.LiveRadio && x.Brands != null).Brands;

        if (brands == null)
            throw new InvalidOperationException("Could not find brands (required for resolving herald id from brand slug)");

        foreach (var brand in brands)
        {
            if (brand.Slug.Equals(stationSlug, StringComparison.InvariantCultureIgnoreCase))
            {
                heraldId = brand.NationalStation.HeraldId;
                break;
            }
        }

        if (heraldId == 0)
            throw new InvalidOperationException($"Could not find herald id for station slug {stationSlug}");

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
        if (message.MessageType != WebSocketMessageType.Text || message.Text == null)
            return;

        try
        {
            var messageData = JsonConvert.DeserializeObject<NowPlayingResponse>(message.Text);

            if (messageData == null || messageData.Type != ServiceMessageType.Station || messageData.NowPlaying == null)
                return;

            // Sometimes the title and artist are null, we can ignore these because another message is sent right after with the correct data
            if (messageData.NowPlaying.Title == null || messageData.NowPlaying.Artist == null)
                return;

            CurrentSong = new SongInfo(messageData.NowPlaying.Title, messageData.NowPlaying.Artist);
        }
        catch
        {
            return;
        }
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
