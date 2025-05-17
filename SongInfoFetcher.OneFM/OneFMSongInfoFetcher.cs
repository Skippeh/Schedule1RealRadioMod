using System;
using System.Threading.Tasks;
using Websocket.Client;

namespace SongInfoFetcher.OneFM;

public class OneFMSongInfoFetcher : WSSongInfoFetcher
{
    public override bool CanListenForSongInfo => true;
    public override bool CanRequestSongInfo => true;

    public OneFMSongInfoFetcher(Uri uri) : base(CreateWSUri(uri))
    {
    }

    private static Uri CreateWSUri(Uri uri)
    {
        throw new NotImplementedException();
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
