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

    /// <summary>Tirage pondere sans remise, pour proposer un apercu des prochaines images (ne modifie pas l'etat).</summary>
    public List<SpotlightImage> SelectMultiple(CatalogState state, int count, int likeWeightMultiplier)
    {
        var remaining = state.Images.Where(i => i.Hash != state.CurrentImageHash).ToList();
        var result = new List<SpotlightImage>();

        for (int i = 0; i < count && remaining.Count > 0; i++)
        {
            var weighted = remaining.Select(img => (Image: img, Weight: img.Liked ? Math.Max(1, likeWeightMultiplier) : 1)).ToList();
            int totalWeight = weighted.Sum(w => w.Weight);
            int roll = _random.Next(totalWeight);
            int cumulative = 0;

            var picked = weighted[^1].Image;
            foreach (var (image, weight) in weighted)
            {
                cumulative += weight;
                if (roll < cumulative) { picked = image; break; }
            }

            result.Add(picked);
            remaining.Remove(picked);
        }

        return result;
    }
}
