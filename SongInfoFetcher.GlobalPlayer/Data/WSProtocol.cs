using System.Collections.Generic;
using Newtonsoft.Json;

namespace SongInfoFetcher.GlobalPlayer.Data;

internal abstract record RequestAction(string Type);

internal record SubscribeRequest(int Service) : RequestAction("subscribe");

internal record ActionsRequest(List<RequestAction> Actions);

internal abstract record ServiceResponse(int Service, ServiceMessageType Type);

internal record NowPlayingResponse(int Service, [JsonProperty("now_playing")] SongData NowPlaying) : ServiceResponse(Service, ServiceMessageType.Station);

internal enum ServiceMessageType
{
    Unknown,
    Station,
}

internal record SongData(string? Title, string? Artist);
