namespace SpotlightDesktop.Models;

public sealed class CatalogState
{
    public int SchemaVersion { get; set; } = 1;
    public string? CurrentImageHash { get; set; }
    public List<SpotlightImage> Images { get; set; } = new();
    public HashSet<string> BlacklistedHashes { get; set; } = new();
}
