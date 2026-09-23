using System.IO;
using System.Text.Json;
using SpotlightDesktop.Models;

namespace SpotlightDesktop.Services;

public sealed class CatalogStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public CatalogStore(string path)
    {
        _path = path;
    }

    public async Task<CatalogState> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
            return new CatalogState();

        await using var stream = File.OpenRead(_path);
        var state = await JsonSerializer.DeserializeAsync<CatalogState>(stream, JsonOptions, ct);
        return state ?? new CatalogState();
    }

    public async Task SaveAsync(CatalogState state, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var tempPath = _path + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, state, JsonOptions, ct);
            }
            File.Copy(tempPath, _path, overwrite: true);
            File.Delete(tempPath);
        }
        finally
        {
            _lock.Release();
        }
    }
}
