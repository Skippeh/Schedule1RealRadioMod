using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SocketIOClient;
using SongInfoFetcher.TruckersFM.Data;

namespace SongInfoFetcher.TruckersFM;

public class TruckersFMSongFetcher : SocketIOSongInfoFetcher
{
    internal static readonly Regex UriRegex = new Regex(@"^https://live\.truckers\.fm(.*)$");
    private const string CurrentSongUrl = "https://radiocloud.pro/api/public/v1/song/current";

    public override bool CanListenForSongInfo => true;
    public override bool CanRequestSongInfo => true;

    public TruckersFMSongFetcher() : base(new Uri("https://public-socket.truckers.fm"), GetSocketIOOptions())
    {
    }

    public override Task Start()
    {
        Client.On("song", OnSongInfoReceived);
        return base.Start();
    }

    private static SocketIOOptions GetSocketIOOptions()
    {
        var options = new SocketIOOptions
        {
            ExtraHeaders = new()
            {
                { "Origin", "https://truckers.fm" }
            },
            AutoUpgrade = true,
            EIO = SocketIO.Core.EngineIO.V3,
            Reconnection = true,
            ReconnectionDelay = 5000,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
        };

        return options;
    }

    private void OnSongInfoReceived(SocketIOResponse response)
    {
        SongEventData songEventData = response.GetValue<SongEventData>();

        var songData = songEventData?.CurrentSong?.Song;

        if (songData == null)
            return;

        CurrentSong = new SongInfo(songData.Title, songData.Artist);
        SongInfoReceived?.Invoke(CurrentSong);
    }

    public override async Task<SongInfo> InternalRequestSongInfo()
    {
        if (CurrentSong != null)
            return CurrentSong;

        HttpResponseMessage response = await Client.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, CurrentSongUrl), default);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<HttpCurrentSongResponse>(json) ?? throw new HttpRequestException("Deserialized JSON is null");

        if (data.Status != "success")
            throw new HttpRequestException($"Status is not 'success': {data.Status}");

        if (data.Data == null)
            throw new HttpRequestException("No data in response");

        CurrentSong = new SongInfo(data.Data.Title, data.Data.Artist);
        SongInfoReceived?.Invoke(CurrentSong);
        return CurrentSong;
    }
}
