using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpotlightDesktop.Models;

namespace SpotlightDesktop.Services;

public sealed class RotationHostedService : BackgroundService
{
    private readonly SpotlightEngine _engine;
    private readonly AppSettings _settings;
    private readonly ILogger<RotationHostedService> _logger;
    private readonly SemaphoreSlim _resetSignal = new(0);

    public RotationHostedService(SpotlightEngine engine, AppSettings settings, ILogger<RotationHostedService> logger)
    {
        _engine = engine;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>Redemarre le compte a rebours de la rotation automatique (appele apres une action manuelle).</summary>
    public void ResetTimer() => _resetSignal.Release();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _engine.InitializeAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = TimeSpan.FromMinutes(_settings.RotationIntervalMinutes);
            var delayTask = Task.Delay(interval, stoppingToken);
            var resetTask = _resetSignal.WaitAsync(stoppingToken);

            var completed = await Task.WhenAny(delayTask, resetTask);
            if (stoppingToken.IsCancellationRequested) break;

            if (completed == delayTask)
            {
                try
                {
                    await _engine.ShowNextAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la rotation automatique.");
                }
            }
            // Si completed == resetTask : une action manuelle vient d'avoir lieu, on relance simplement le minuteur.
        }
    }
}
