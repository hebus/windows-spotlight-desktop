using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpotlightDesktop.Models;

namespace SpotlightDesktop.Services;

public sealed class SpotlightApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const int MaxAttempts = 2;

    private readonly HttpClient _httpClient;
    private readonly ILogger<SpotlightApiClient> _logger;

    public SpotlightApiClient(HttpClient httpClient, ILogger<SpotlightApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<SpotlightAd>> FetchBatchAsync(string country, string locale, int batchCount, CancellationToken ct)
    {
        var url = $"https://fd.api.iris.microsoft.com/v4/api/selection?placement=88000820&bcnt={batchCount}" +
                  $"&country={Uri.EscapeDataString(country)}&locale={Uri.EscapeDataString(locale)}&fmt=json";

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<SpotlightApiResponse>(url, JsonOptions, ct);
                var items = response?.Batchrsp?.Items;
                if (items is null || items.Count == 0)
                {
                    _logger.LogWarning("Reponse API Spotlight sans image exploitable.");
                    return new List<SpotlightAd>();
                }

                var ads = new List<SpotlightAd>();
                foreach (var rawItem in items)
                {
                    if (string.IsNullOrEmpty(rawItem.Item)) continue;

                    var envelope = JsonSerializer.Deserialize<SpotlightAdEnvelope>(rawItem.Item, JsonOptions);
                    if (envelope?.Ad?.LandscapeImage?.Asset is not null)
                        ads.Add(envelope.Ad);
                }

                return ads;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(ex, "Echec appel API Spotlight (tentative {Attempt}/{Max}), nouvelle tentative dans 10s.", attempt, MaxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }
        }

        _logger.LogError("Abandon de l'appel API Spotlight apres {Max} tentatives.", MaxAttempts);
        return new List<SpotlightAd>();
    }
}
