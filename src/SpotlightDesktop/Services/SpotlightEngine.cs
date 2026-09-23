using System.IO;
using Microsoft.Extensions.Logging;
using SpotlightDesktop.Models;

namespace SpotlightDesktop.Services;

public sealed class SpotlightEngine
{
    private readonly SpotlightApiClient _apiClient;
    private readonly ImageDownloadService _downloadService;
    private readonly CatalogStore _catalogStore;
    private readonly RetentionService _retentionService;
    private readonly RotationSelector _rotationSelector;
    private readonly WallpaperCompositor _compositor;
    private readonly WallpaperSetter _wallpaperSetter;
    private readonly AppSettings _settings;
    private readonly ILogger<SpotlightEngine> _logger;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    private CatalogState _state = new();
    private bool _useWallpaperA = true;

    public event Action<SpotlightImage>? CurrentImageChanged;

    public SpotlightEngine(
        SpotlightApiClient apiClient,
        ImageDownloadService downloadService,
        CatalogStore catalogStore,
        RetentionService retentionService,
        RotationSelector rotationSelector,
        WallpaperCompositor compositor,
        WallpaperSetter wallpaperSetter,
        AppSettings settings,
        ILogger<SpotlightEngine> logger)
    {
        _apiClient = apiClient;
        _downloadService = downloadService;
        _catalogStore = catalogStore;
        _retentionService = retentionService;
        _rotationSelector = rotationSelector;
        _compositor = compositor;
        _wallpaperSetter = wallpaperSetter;
        _settings = settings;
        _logger = logger;
        Directory.CreateDirectory(AppPaths.WallpaperFolder);
    }

    public SpotlightImage? CurrentImage => _state.Images.FirstOrDefault(i => i.Hash == _state.CurrentImageHash);

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _state = await _catalogStore.LoadAsync(ct);

        if (_state.Images.Count == 0)
        {
            await RefreshFromApiAsync(ct);
        }

        var toApply = CurrentImage ?? _rotationSelector.SelectNext(_state, _settings.LikeWeightMultiplier);
        if (toApply is not null)
        {
            await ApplyImageAsync(toApply, ct);
        }
    }

    public async Task RefreshFromApiAsync(CancellationToken ct = default)
    {
        var ads = await _apiClient.FetchBatchAsync(_settings.Country, _settings.Locale, _settings.BatchCount, ct);
        var knownHashes = _state.Images.Select(i => i.Hash).ToHashSet();

        foreach (var ad in ads)
        {
            var image = await _downloadService.DownloadAsync(ad, knownHashes, _state.BlacklistedHashes, ct);
            if (image is not null)
            {
                _state.Images.Add(image);
                knownHashes.Add(image.Hash);
            }
        }

        _retentionService.Enforce(_state, _settings.MaxImages);
        await _catalogStore.SaveAsync(_state, ct);
    }

    public async Task ShowNextAsync(CancellationToken ct = default)
    {
        await _mutex.WaitAsync(ct);
        try
        {
            var pending = _state.Images.Count(i => i.Hash != _state.CurrentImageHash);
            if (pending < 3)
            {
                await RefreshFromApiAsync(ct);
            }

            var next = _rotationSelector.SelectNext(_state, _settings.LikeWeightMultiplier);
            await ApplyImageAsync(next, ct);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task LikeCurrentAsync(CancellationToken ct = default)
    {
        var current = CurrentImage;
        if (current is null) return;

        current.Liked = true;
        await _catalogStore.SaveAsync(_state, ct);
    }

    public async Task DislikeCurrentAsync(CancellationToken ct = default)
    {
        var current = CurrentImage;
        if (current is null) return;

        _state.BlacklistedHashes.Add(current.Hash);
        _state.Images.Remove(current);

        var path = Path.Combine(AppPaths.ImagesFolder, current.FileName);
        if (File.Exists(path)) File.Delete(path);

        await _catalogStore.SaveAsync(_state, ct);
        await ShowNextAsync(ct);
    }

    private async Task ApplyImageAsync(SpotlightImage? image, CancellationToken ct)
    {
        if (image is null) return;

        var sourcePath = Path.Combine(AppPaths.ImagesFolder, image.FileName);
        if (!File.Exists(sourcePath))
        {
            _logger.LogWarning("Fichier source introuvable pour {Hash}, entree retiree du catalogue.", image.Hash);
            _state.Images.Remove(image);
            await _catalogStore.SaveAsync(_state, ct);
            return;
        }

        _useWallpaperA = !_useWallpaperA;
        var destPath = Path.Combine(AppPaths.WallpaperFolder, _useWallpaperA ? "current-a.jpg" : "current-b.jpg");

        _compositor.Compose(sourcePath, destPath, image);
        _wallpaperSetter.Apply(destPath);

        image.LastShownAt = DateTimeOffset.UtcNow;
        image.ShowCount++;
        _state.CurrentImageHash = image.Hash;
        await _catalogStore.SaveAsync(_state, ct);

        CurrentImageChanged?.Invoke(image);
    }
}
