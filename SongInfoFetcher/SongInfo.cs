namespace SongInfoFetcher;

public record SongInfo(string Title, string? Artist)
{
    public override string ToString()
    {
        if (Artist == null)
            return Title;

        return $"{Artist} - {Title}";
    }
}
