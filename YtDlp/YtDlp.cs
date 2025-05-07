using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HashUtility;
using Newtonsoft.Json;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

public class YtDlp
{
    private YoutubeDL youtubeDL;

    private readonly string cachePath;
    private readonly string binariesPath;
    private readonly string audioFilesPath;
    private readonly string ytDlpCachePath;
    private Task? downloadBinariesTask;
    private bool binariesDownloaded;

    private object downloadMetaLock = new();
    private object downloadAudioFileLock = new();

    private Dictionary<string, Task<VideoData>> downloadMetaTasks = [];
    private Dictionary<string, Task<string>> downloadAudioFileTasks = [];

    public YtDlp(string cachePath, byte maxNumberOfProcesses = 4)
    {
        this.cachePath = cachePath;
        binariesPath = Path.Combine(cachePath, "bin");
        audioFilesPath = Path.Combine(cachePath, "audio");
        ytDlpCachePath = Path.Combine(cachePath, "yt-dlp");

        youtubeDL = new(maxNumberOfProcesses)
        {
            FFmpegPath = Path.Combine(binariesPath, Utils.FfmpegBinaryName),
            YoutubeDLPath = Path.Combine(binariesPath, Utils.YtDlpBinaryName),
            OutputFolder = Path.Combine(cachePath, "audio"),
        };

        if (!Directory.Exists(binariesPath))
            Directory.CreateDirectory(binariesPath);

        if (!Directory.Exists(audioFilesPath))
            Directory.CreateDirectory(audioFilesPath);

        if (!Directory.Exists(ytDlpCachePath))
            Directory.CreateDirectory(ytDlpCachePath);

        downloadBinariesTask = DownloadBinaries();
    }

    /// <summary>
    /// Downloads the binaries if they haven't been downloaded yet. This task is started automatically from the constructor, but it can be awaited by calling this method if needed.
    /// </summary>
    /// <returns></returns>
    public async Task DownloadBinaries()
    {
        if (downloadBinariesTask != null)
        {
            await downloadBinariesTask;
            return;
        }

        if (binariesDownloaded)
            return;

        await Task.Run(async () =>
        {
            await Utils.DownloadBinaries(skipExisting: true, directoryPath: binariesPath);
            binariesDownloaded = true;
        });
    }

    public Task<string> DownloadAudioFile(string url, CancellationToken cancellationToken, IProgress<DownloadProgress>? progress = null, IProgress<string>? output = null)
    {
        lock (downloadAudioFileLock)
        {
            if (downloadAudioFileTasks.TryGetValue(url, out var downloadAudioFileTask))
                return downloadAudioFileTask;

            var task = Task.Run(async () =>
            {
                await DownloadBinaries();

                var urlHash = HashUrl(url);
                string filePath = $"{urlHash}.mp3";

                if (File.Exists(Path.Combine(audioFilesPath, filePath)))
                    return Path.Combine(audioFilesPath, filePath);

                var options = new OptionSet()
                {
                    CacheDir = ytDlpCachePath,
                    AudioQuality = 3,
                    Output = Path.Combine(audioFilesPath, filePath),
                };

                RunResult<string> result = await youtubeDL.RunAudioDownload(url, AudioConversionFormat.Mp3, cancellationToken, progress, output, options);

                if (!result.Success)
                {
                    throw new YtDlpVideoDownloadException(result.ErrorOutput);
                }

                return result.Data;
            });

            downloadAudioFileTasks[url] = task;
            return task;
        }
    }

    public Task<VideoData> DownloadMetaData(string url, CancellationToken cancellationToken)
    {
        lock (downloadMetaLock)
        {
            if (downloadMetaTasks.TryGetValue(url, out var downloadMetaTask))
                return downloadMetaTask;

            var task = Task.Run(async () =>
            {
                await DownloadBinaries();
                uint urlHash = HashUrl(url);
                string filePath = Path.Combine(audioFilesPath, $"{urlHash}.json");
                RunResult<VideoData> result;

                if (!File.Exists(Path.Combine(audioFilesPath, $"{urlHash}.json")))
                {
                    result = await youtubeDL.RunVideoDataFetch(url, cancellationToken);

                    if (!result.Success)
                        throw new YtDlpFetchMetaDataException(result.ErrorOutput);

                    await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(result.Data));
                }
                else
                {
                    VideoData videoData;

                    try
                    {
                        // Try to read deserialize cached video data
                        videoData = JsonConvert.DeserializeObject<VideoData>(await File.ReadAllTextAsync(filePath))!;
                    }
                    catch
                    {
                        // Re-fetch video data if it fails
                        result = await youtubeDL.RunVideoDataFetch(url, cancellationToken);

                        if (!result.Success)
                            throw new YtDlpFetchMetaDataException(result.ErrorOutput);

                        // Save fetched video data again
                        await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(result.Data));
                        videoData = result.Data;
                    }

                    result = new RunResult<VideoData>(success: true, error: [], result: videoData);
                }

                return result.Data;
            });

            downloadMetaTasks[url] = task;
            return task;
        }
    }

    private uint HashUrl(string url)
    {
        var uri = new Uri(url);
        url = uri.Host + uri.PathAndQuery;
        return uri.PathAndQuery.GetStableHashCode();
    }
}
