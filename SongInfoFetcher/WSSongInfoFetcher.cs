using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Websocket.Client;

namespace SongInfoFetcher;

public abstract class WSSongInfoFetcher : ISongInfoFetcher
{
    public abstract bool CanListenForSongInfo { get; }
    public abstract bool CanRequestSongInfo { get; }

    protected Action<SongInfo>? SongInfoReceived { get; private set; }

    private WebsocketClient client;

    /// <summary>
    /// Instantiate a new WSSongInfoFetcher
    /// </summary>
    /// <param name="uri">The URI to connect to</param>
    /// <param name="clientFactory">Optional custom client factory</param>
    /// <param name="reconnectTimeout">
    /// Optionally set the reconnect timeout.
    /// This is the maximum amount of time the client will stay connected to the server without receiving any messages.
    /// Default is 1 minute.
    /// </param>
    public WSSongInfoFetcher(Uri uri, Func<ClientWebSocket>? clientFactory = null, Action<WebsocketClient>? configureClient = null, TimeSpan? reconnectTimeout = null)
    {
        reconnectTimeout ??= TimeSpan.FromMinutes(1);
        client = new(uri, clientFactory);

        if (reconnectTimeout != null)
            client.ReconnectTimeout = reconnectTimeout;

        client.ErrorReconnectTimeout = TimeSpan.FromSeconds(5);
        client.MessageReceived.Subscribe(OnMessageReceived);
        client.ReconnectionHappened.Subscribe(OnReconnected);

        configureClient?.Invoke(client);

        client.Start();
    }

    public Task<SongInfo> RequestSongInfo()
    {
        if (!CanRequestSongInfo)
            throw new InvalidOperationException("Cannot request song info");

        return InternalRequestSongInfo();
    }

    public void SubscribeToSongInfoChanges(Action<SongInfo> onSongInfoChanged)
    {
        if (onSongInfoChanged == null)
            throw new ArgumentNullException(nameof(onSongInfoChanged));

        if (!CanListenForSongInfo)
            throw new InvalidOperationException("Cannot listen for song info");

        SongInfoReceived += onSongInfoChanged;
    }

    public void UnsubscribeFromSongInfoChanges(Action<SongInfo> onSongInfoChanged)
    {
        if (onSongInfoChanged == null)
            throw new ArgumentNullException(nameof(onSongInfoChanged));

        if (!CanListenForSongInfo)
            throw new InvalidOperationException("Cannot listen for song info");

        SongInfoReceived -= onSongInfoChanged;
    }

    protected void Send(string text)
    {
        client.Send(text);
    }

    protected void Send(byte[] data)
    {
        client.Send(data);
    }

    protected abstract void OnReconnected(ReconnectionInfo info);
    protected abstract void OnMessageReceived(ResponseMessage message);
    protected abstract Task<SongInfo> InternalRequestSongInfo();
}
