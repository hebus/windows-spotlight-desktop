namespace SpotlightDesktop.Models;

public sealed class SpotlightApiResponse
{
    public BatchResponse? Batchrsp { get; set; }
}

public sealed class BatchResponse
{
    public List<BatchItem>? Items { get; set; }
}

public sealed class BatchItem
{
    public string? Item { get; set; }
}

public sealed class SpotlightAdEnvelope
{
    public SpotlightAd? Ad { get; set; }
}

public sealed class SpotlightAd
{
    public SpotlightAsset? LandscapeImage { get; set; }
    public SpotlightAsset? PortraitImage { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Copyright { get; set; }
    public string? IconHoverText { get; set; }
    public string? CtaText { get; set; }
    public string? CtaUri { get; set; }
    public string? EntityId { get; set; }
}

public sealed class SpotlightAsset
{
    public string? Asset { get; set; }
}
