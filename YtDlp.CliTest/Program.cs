using System;
using System.Threading.Tasks;
using YoutubeDLSharp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: YtDlp.CliTest.exe <cachePath> <url>");
            return;
        }

        string cachePath = args[0];
        string url = args[1];

        var ytDlp = new YtDlp(cachePath);
        var metaData = await ytDlp.DownloadMetaData(url, default);

        Console.WriteLine($"Video name: {metaData.Data.Title}");

        var result = await ytDlp.DownloadAudioFile(url, default, new OutputDownloadProgress(), new OutputProgress());

        Console.WriteLine($"Success: {result.Success}, Data: {result.Data}, ErrorOutput: {string.Join("\n", result.ErrorOutput)}");
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
