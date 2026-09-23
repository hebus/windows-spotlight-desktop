using SpotlightDesktop.Models;

namespace SpotlightDesktop.Services;

public sealed class RotationSelector
{
    private readonly Random _random = new();

    public SpotlightImage? SelectNext(CatalogState state, int likeWeightMultiplier)
    {
        var pool = state.Images.Where(i => i.Hash != state.CurrentImageHash).ToList();
        if (pool.Count == 0)
            pool = state.Images.ToList();
        if (pool.Count == 0)
            return null;

        var weighted = pool.Select(i => (Image: i, Weight: i.Liked ? Math.Max(1, likeWeightMultiplier) : 1)).ToList();
        int totalWeight = weighted.Sum(w => w.Weight);
        int roll = _random.Next(totalWeight);
        int cumulative = 0;

        foreach (var (image, weight) in weighted)
        {
            cumulative += weight;
            if (roll < cumulative) return image;
        }

        return weighted[^1].Image;
    }
}
