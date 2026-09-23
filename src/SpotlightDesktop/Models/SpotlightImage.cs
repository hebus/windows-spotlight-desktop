namespace SpotlightDesktop.Models;

public sealed class SpotlightImage
{
    public string Hash { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Copyright { get; set; } = "";
    public string? SourceUrl { get; set; }
    public string? CtaUri { get; set; }
    public DateTimeOffset DownloadedAt { get; set; }
    public bool Liked { get; set; }
    public DateTimeOffset? LastShownAt { get; set; }
    public int ShowCount { get; set; }
}
