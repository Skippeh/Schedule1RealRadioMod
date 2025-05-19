using System;
using System.Threading.Tasks;

namespace SongInfoFetcher;

public interface ISongInfoFetcher : IDisposable
{
    /// <summary>
    /// Checks if the song info fetcher can listen for song info. This means the server sends new song info to the client when the song changes.
    /// </summary>
    bool CanListenForSongInfo { get; }

    /// <summary>
    /// Checks if the song info fetcher can request song info. This means the client sends a request to the server to get the current song info.
    /// </summary>
    bool CanRequestSongInfo { get; }

    /// <summary>
    /// The current song info. This is only useable if <see cref="CanListenForSongInfo" /> is true.
    /// </summary>
    public SongInfo? CurrentSong { get; }

    public Task Start();
    public Task Stop();

    /// <summary>
    /// Request the current song info from the server.
    /// </summary>
    Task<SongInfo> RequestSongInfo();

    /// <summary>
    /// Subscribe to song info changes.
    /// </summary>
    /// <param name="onSongInfoChanged"></param>
    void SubscribeToSongInfoChanges(Action<SongInfo> onSongInfoChanged);

    /// <summary>
    /// Unsubscribe from song info changes.
    /// </summary>
    /// <param name="onSongInfoChanged"></param>
    void UnsubscribeFromSongInfoChanges(Action<SongInfo> onSongInfoChanged);
}
