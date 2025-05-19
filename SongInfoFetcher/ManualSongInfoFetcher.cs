using System;
using System.Threading.Tasks;

namespace SongInfoFetcher;

/// <summary>
/// A manual song info fetcher. Can be used when there is a need to manually set the current song info.
/// </summary>
public class ManualSongInfoFetcher : ISongInfoFetcher
{
    private SongInfo? currentSong;

    public bool CanListenForSongInfo => true;

    public bool CanRequestSongInfo => false;

    public SongInfo? CurrentSong
    {
        get => currentSong;
        set
        {
            if (currentSong == value)
                return;

            currentSong = value;

            if (value != null)
                onSongInfoChanged?.Invoke(value);
        }
    }

    private Action<SongInfo>? onSongInfoChanged;

    public void Dispose()
    {
    }

    public Task<SongInfo> RequestSongInfo()
    {
        throw new NotSupportedException();
    }

    public Task Start() => Task.CompletedTask;
    public Task Stop() => Task.CompletedTask;

    public void SubscribeToSongInfoChanges(Action<SongInfo> onSongInfoChanged)
    {
        this.onSongInfoChanged += onSongInfoChanged;
    }

    public void UnsubscribeFromSongInfoChanges(Action<SongInfo> onSongInfoChanged)
    {
        this.onSongInfoChanged -= onSongInfoChanged;
    }
}
