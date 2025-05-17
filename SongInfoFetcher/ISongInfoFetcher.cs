using System;
using System.Threading.Tasks;

namespace SongInfoFetcher;

public interface ISongInfoFetcher
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
    /// Request the current song info from the server.
    /// </summary>
    Task<SongInfo> RequestSongInfo();

    /// <summary>
    /// Subscribe to song info changes. Note: This is not invoked when <see cref="RequestSongInfo"/> is called.
    /// </summary>
    /// <param name="onSongInfoChanged"></param>
    void SubscribeToSongInfoChanges(Action<SongInfo> onSongInfoChanged);

    /// <summary>
    /// Unsubscribe from song info changes.
    /// </summary>
    /// <param name="onSongInfoChanged"></param>
    void UnsubscribeFromSongInfoChanges(Action<SongInfo> onSongInfoChanged);
}
