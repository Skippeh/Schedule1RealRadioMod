using System;
using System.Linq;
using System.Threading.Tasks;
using YoutubeDLSharp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: YtDlp.CliTest.exe <cachePath> <type> <urls>");
            return;
        }

        string cachePath = args[0];
        string type = args[1];
        string[] urls = args.Skip(2).ToArray();

        var ytDlp = new YtDlp(cachePath);

        if (type == "video")
        {
            foreach (var url in urls)
            {
                var metaData = await ytDlp.DownloadMetaData(url, default);

                Console.WriteLine($"Video name: {metaData.Title}");

                string filePath = await ytDlp.DownloadAudioFile(url, default, new OutputDownloadProgress(), new OutputProgress());

                Console.WriteLine($"Audio file path: {filePath}");
            }
        }
        else if (type == "playlist")
        {
            var videoDatas = await ytDlp.DownloadPlaylistUrls(urls, default);

            foreach (var videoData in videoDatas)
            {
                Console.WriteLine($"{videoData.Url}: {videoData.Title}");
            }
        }
        else
        {
            throw new ArgumentException($"Invalid type: {type}");
        }
    }
}

class OutputDownloadProgress : IProgress<DownloadProgress>
{
    public void Report(DownloadProgress value)
    {
        Console.WriteLine($"State: {value.State}, Percent: {value.Progress}, Speed: {value.DownloadSpeed}, ETA: {value.ETA}");
    }
}

class OutputProgress : IProgress<string>
{
    public void Report(string value)
    {
        Console.WriteLine(value);
    }
}
