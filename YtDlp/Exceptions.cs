using System;

public class YtDlpException(string[] errors) : Exception($"The YtDlp process exited with errors:\n\t{ConcatErrors(errors)}")
{
    public string[] Errors { get; set; } = errors;

    private static string ConcatErrors(string[] errors) => string.Join("\n\t", errors);
}

public class YtDlpVideoDownloadException(string[] errors) : YtDlpException(errors)
{
}

public class YtDlpFetchMetaDataException(string[] errors) : YtDlpException(errors)
{
}
