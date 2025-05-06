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

    private Dictionary<string, Task> downloadTasks = [];

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

    private async Task DownloadBinaries()
    {
        if (downloadBinariesTask != null)
        {
            await downloadBinariesTask;
            return;
        }

        if (binariesDownloaded)
            return;

        await Utils.DownloadBinaries(skipExisting: true, directoryPath: binariesPath);
        binariesDownloaded = true;
    }

    public async Task<RunResult<string>> DownloadAudioFile(string url, CancellationToken cancellationToken, IProgress<DownloadProgress>? progress, IProgress<string>? output = null)
    {
        await DownloadBinaries();

        var urlHash = HashUrl(url);
        string filePath = $"{urlHash}.mp3";

        if (File.Exists(Path.Combine(audioFilesPath, filePath)))
            return new RunResult<string>(true, error: [], result: Path.Combine(audioFilesPath, filePath));

        var options = new OptionSet()
        {
            CacheDir = ytDlpCachePath,
            AudioQuality = 3,
            Output = Path.Combine(audioFilesPath, filePath),
        };

        return await youtubeDL.RunAudioDownload(url, AudioConversionFormat.Mp3, cancellationToken, progress, output, options);
    }

    public async Task<RunResult<VideoData>> DownloadMetaData(string url, CancellationToken cancellationToken)
    {
        await DownloadBinaries();
        uint urlHash = HashUrl(url);
        string filePath = Path.Combine(audioFilesPath, $"{urlHash}.json");
        RunResult<VideoData> result;
        if (!File.Exists(Path.Combine(audioFilesPath, $"{urlHash}.json")))
        {
            result = await youtubeDL.RunVideoDataFetch(url, cancellationToken);

            if (result.Success)
            {
                await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(result.Data));
            }
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

                // Save fetched video data again
                await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(result.Data));
                videoData = result.Data;
            }

            result = new RunResult<VideoData>(true, error: [], result: videoData);
        }

        return result;
    }

    private uint HashUrl(string url)
    {
        var uri = new Uri(url);
        url = uri.Host + uri.PathAndQuery;
        return uri.PathAndQuery.GetStableHashCode();
    }
}
