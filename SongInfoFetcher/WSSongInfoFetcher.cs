using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Websocket.Client;

namespace SongInfoFetcher;

public abstract class WSSongInfoFetcher : ISongInfoFetcher
{
    public Uri? WSUri { get; set; }

    public abstract bool CanListenForSongInfo { get; }
    public abstract bool CanRequestSongInfo { get; }

    protected Action<SongInfo>? SongInfoReceived { get; private set; }
    protected WebsocketClient Client { get; private set; }

    public SongInfo? CurrentSong { get; protected set; }

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
    public WSSongInfoFetcher(Func<ClientWebSocket>? clientFactory = null, Action<WebsocketClient>? configureClient = null, TimeSpan? reconnectTimeout = null)
    {
        reconnectTimeout ??= TimeSpan.FromMinutes(1);

        // this uri doesn't matter, it just needs to be something
        Client = new(new Uri("ws://localhost"), clientFactory);

        if (reconnectTimeout != null)
            Client.ReconnectTimeout = reconnectTimeout;

        Client.ErrorReconnectTimeout = TimeSpan.FromSeconds(5);
        Client.MessageReceived.Subscribe(OnMessageReceived);
        Client.ReconnectionHappened.Subscribe(OnReconnected);
        Client.DisconnectionHappened.Subscribe(OnDisconnected);

        configureClient?.Invoke(Client);
    }

    public virtual async Task Start()
    {
        if (WSUri == null)
            throw new InvalidOperationException("WSUri is not set");

        if (CanListenForSongInfo)
        {
            SongInfoReceived += OnSongInfoReceived;
        }

        Client.Url = WSUri;
        await Client.Start();
    }

    private void OnSongInfoReceived(SongInfo info)
    {
        CurrentSong = info;
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
        Client.Send(text);
    }

    protected void Send(byte[] data)
    {
        Client.Send(data);
    }

    protected abstract void OnReconnected(ReconnectionInfo info);
    protected abstract void OnMessageReceived(ResponseMessage message);
    protected abstract Task<SongInfo> InternalRequestSongInfo();
    protected abstract void OnDisconnected(DisconnectionInfo info);
}
