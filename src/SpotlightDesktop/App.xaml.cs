using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpotlightDesktop.Models;
using SpotlightDesktop.Services;
using SpotlightDesktop.UI;

namespace SpotlightDesktop;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private IHost? _host;
    private TrayIconManager? _trayIconManager;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Directory.CreateDirectory(AppPaths.RootFolder);
        Directory.CreateDirectory(AppPaths.LogsFolder);

        DispatcherUnhandledException += (_, args) => LogCrash("DispatcherUnhandledException", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) => LogCrash("AppDomainUnhandledException", args.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogCrash("UnobservedTaskException", args.Exception);
            args.SetObserved();
        };

        _mutex = new Mutex(initiallyOwned: true, @"Global\SpotlightDesktop-SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show("Windows Spotlight Desktop est deja en cours d'execution.", "Spotlight Desktop");
            Shutdown();
            return;
        }

        var settings = await LoadOrCreateSettingsAsync();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(settings);
                services.AddHttpClient<SpotlightApiClient>();
                services.AddHttpClient<ImageDownloadService>();
                services.AddSingleton(_ => new CatalogStore(AppPaths.CatalogFile));
                services.AddSingleton<RetentionService>();
                services.AddSingleton<RotationSelector>();
                services.AddSingleton<MonitorService>();
                services.AddSingleton<WallpaperCompositor>();
                services.AddSingleton<WallpaperSetter>();
                services.AddSingleton<StartupManager>();
                services.AddSingleton<SpotlightEngine>();
                services.AddSingleton<RotationHostedService>();
                services.AddHostedService(sp => sp.GetRequiredService<RotationHostedService>());
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new FileLoggerProvider(Path.Combine(AppPaths.LogsFolder, $"app-{DateTime.Now:yyyyMMdd}.log")));
            })
            .Build();

        await _host.StartAsync();

        var engine = _host.Services.GetRequiredService<SpotlightEngine>();
        var rotationService = _host.Services.GetRequiredService<RotationHostedService>();
        var startupManager = _host.Services.GetRequiredService<StartupManager>();

        _trayIconManager = new TrayIconManager(engine, rotationService, startupManager, RequestExit);
    }

    private void RequestExit() => Shutdown();

    protected override async void OnExit(ExitEventArgs e)
    {
        _trayIconManager?.Dispose();

        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        _mutex?.ReleaseMutex();
        base.OnExit(e);
    }

    private static async Task<AppSettings> LoadOrCreateSettingsAsync()
    {
        if (File.Exists(AppPaths.SettingsFile))
        {
            await using var stream = File.OpenRead(AppPaths.SettingsFile);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream);
            if (loaded is not null) return loaded;
        }

        var defaults = new AppSettings();
        Directory.CreateDirectory(AppPaths.RootFolder);
        await using var writeStream = File.Create(AppPaths.SettingsFile);
        await JsonSerializer.SerializeAsync(writeStream, defaults, new JsonSerializerOptions { WriteIndented = true });
        return defaults;
    }

    private static void LogCrash(string source, object? exceptionObj)
    {
        try
        {
            var path = Path.Combine(AppPaths.LogsFolder, $"crash-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log");
            Directory.CreateDirectory(AppPaths.LogsFolder);
            File.WriteAllText(path, $"[{source}]{Environment.NewLine}{exceptionObj}");
        }
        catch
        {
            // Rien de mieux a faire si meme l'ecriture du log de crash echoue.
        }
    }
}
