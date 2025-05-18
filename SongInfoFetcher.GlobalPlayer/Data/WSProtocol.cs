using System.Collections.Generic;

namespace SongInfoFetcher.GlobalPlayer;

public abstract record RequestAction(string Type);

public record SubscribeRequest(int Service) : RequestAction("subscribe");

public record ActionsRequest(List<RequestAction> Actions);
