using System;
using System.Net;
using System.Threading.Tasks;

namespace SongInfoFetcher;

public abstract class HttpRequestSongInfoFetcher : ISongInfoFetcher
{
    public bool CanListenForSongInfo => false;
    public bool CanRequestSongInfo => true;

    public SongInfo? CurrentSong
    {
        get => currentSong;
        protected set
        {
            if (currentSong == value)
                return;

            currentSong = value;

            if (value != null)
                onSongInfoChanged?.Invoke(value);
        }
    }

    private SongInfo? currentSong;

    protected readonly WebClient WebClient = new();

    private Action<SongInfo>? onSongInfoChanged;

    public void Dispose()
    {
        WebClient.Dispose();
    }

    public Task Start() => Task.CompletedTask;
    public Task Stop() => Task.CompletedTask;

    public abstract Task<SongInfo> RequestSongInfo();

    public void SubscribeToSongInfoChanges(Action<SongInfo> onSongInfoChanged)
    {
        this.onSongInfoChanged += onSongInfoChanged;
    }

    public void UnsubscribeFromSongInfoChanges(Action<SongInfo> onSongInfoChanged)
    {
        this.onSongInfoChanged -= onSongInfoChanged;
    }
}
