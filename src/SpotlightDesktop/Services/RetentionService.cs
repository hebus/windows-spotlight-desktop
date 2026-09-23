using System.IO;
using Microsoft.Extensions.Logging;
using SpotlightDesktop.Models;

namespace SpotlightDesktop.Services;

public sealed class RetentionService
{
    private readonly ILogger<RetentionService> _logger;

    public RetentionService(ILogger<RetentionService> logger)
    {
        _logger = logger;
    }

    public void Enforce(CatalogState state, int maxImages)
    {
        int excess = state.Images.Count - maxImages;
        if (excess <= 0) return;

        var candidates = state.Images
            .Where(i => !i.Liked)
            .OrderBy(i => i.DownloadedAt)
            .ToList();

        if (candidates.Count == 0)
        {
            _logger.LogWarning("Retention : toutes les images sont 'aimees', purge de la moins recemment montree pour eviter une croissance illimitee.");
            candidates = state.Images.OrderBy(i => i.LastShownAt ?? i.DownloadedAt).ToList();
        }

        var toRemove = candidates.Take(excess).ToList();
        foreach (var image in toRemove)
        {
            var path = Path.Combine(AppPaths.ImagesFolder, image.FileName);
            if (File.Exists(path)) File.Delete(path);
            state.Images.Remove(image);
            _logger.LogInformation("Supprime par retention : {FileName}", image.FileName);
        }
    }
}
