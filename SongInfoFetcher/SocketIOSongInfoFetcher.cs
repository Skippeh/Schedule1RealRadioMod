using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using SocketIO.Serializer.NewtonsoftJson;
using SocketIOClient;

namespace SongInfoFetcher;

public abstract class SocketIOSongInfoFetcher : ISongInfoFetcher
{

    public abstract bool CanListenForSongInfo { get; }
    public abstract bool CanRequestSongInfo { get; }

    public SongInfo? CurrentSong
    {
        get => currentSong;
        protected set
        {
            if (value == currentSong)
                return;

            currentSong = value;

            if (value != null)
                SongInfoReceived?.Invoke(value);
        }
    }

    protected Action<SongInfo>? SongInfoReceived { get; private set; }
    protected SocketIOClient.SocketIO Client { get; private set; }

    private SongInfo? currentSong;

    public SocketIOSongInfoFetcher(Uri uri, SocketIOOptions? options = null)
    {
        options ??= new SocketIOOptions();
        Client = new(uri, options);
        Client.Serializer = new NewtonsoftJsonSerializer(new Newtonsoft.Json.JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        });
        Client.OnConnected += (_, _) => OnConnected();
        Client.OnDisconnected += (_, _) => OnDisconnected();
    }

    public Task<SongInfo> RequestSongInfo()
    {
        if (!CanRequestSongInfo)
            throw new InvalidOperationException("Cannot request song info");

        return InternalRequestSongInfo();
    }

    public abstract Task<SongInfo> InternalRequestSongInfo();

    public virtual Task Start()
    {
        return Client.ConnectAsync();
    }

    public Task Stop()
    {
        if (!Client.Connected)
            return Task.CompletedTask;

        return Client.DisconnectAsync();
    }

    public void SubscribeToSongInfoChanges(Action<SongInfo> onSongInfoChanged)
    {
        if (onSongInfoChanged == null)
            throw new ArgumentNullException(nameof(onSongInfoChanged));

        SongInfoReceived += onSongInfoChanged;
    }

    public void UnsubscribeFromSongInfoChanges(Action<SongInfo> onSongInfoChanged)
    {
        if (onSongInfoChanged == null)
            throw new ArgumentNullException(nameof(onSongInfoChanged));

        SongInfoReceived -= onSongInfoChanged;
    }

    protected virtual void OnDisconnected()
    {
        CurrentSong = null;
    }

    protected virtual void OnConnected()
    {
    }

    public void Dispose()
    {
        Client.Dispose();
    }
}
