using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SpotlightDesktop.Models;

namespace SpotlightDesktop.Services;

public sealed class ImageDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageDownloadService> _logger;

    public ImageDownloadService(HttpClient httpClient, ILogger<ImageDownloadService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        Directory.CreateDirectory(AppPaths.ImagesFolder);
    }

    public async Task<SpotlightImage?> DownloadAsync(
        SpotlightAd ad, IReadOnlySet<string> knownHashes, IReadOnlySet<string> blacklistedHashes, CancellationToken ct)
    {
        var url = ad.LandscapeImage?.Asset;
        if (string.IsNullOrEmpty(url)) return null;

        byte[] bytes;
        try
        {
            bytes = await _httpClient.GetByteArrayAsync(url, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Echec du telechargement de {Url}", url);
            return null;
        }

        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        if (blacklistedHashes.Contains(hash))
        {
            _logger.LogInformation("Image {Hash} ignoree (liste noire).", hash);
            return null;
        }

        if (knownHashes.Contains(hash))
        {
            _logger.LogInformation("Image {Hash} deja connue, ignoree.", hash);
            return null;
        }

        var fileName = $"{hash}.jpg";
        var path = Path.Combine(AppPaths.ImagesFolder, fileName);
        await File.WriteAllBytesAsync(path, bytes, ct);

        _logger.LogInformation("Telecharge : {FileName} ({Title})", fileName, ad.Title);

        return new SpotlightImage
        {
            Hash = hash,
            FileName = fileName,
            Title = ad.Title ?? string.Empty,
            Description = ad.Description ?? string.Empty,
            Copyright = ad.Copyright ?? string.Empty,
            SourceUrl = url,
            CtaUri = ad.CtaUri,
            DownloadedAt = DateTimeOffset.UtcNow,
            Liked = false,
            ShowCount = 0
        };
    }
}
