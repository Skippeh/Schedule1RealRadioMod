namespace SongInfoFetcher;

public record SongInfo
{
    public string Title { get; set; }
    public string? Artist { get; set; }

    public SongInfo(string? Title, string? Artist)
    {
        this.Title = Title ?? "Unknown Title";
        this.Artist = Artist;
    }

    public override string ToString()
    {
        if (Artist == null)
            return Title;

        return $"{Artist} - {Title}";
    }
}
