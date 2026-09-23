using System.Windows.Forms;
using SpotlightDesktop.Services;

namespace SpotlightDesktop.UI;

public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SpotlightEngine _engine;
    private readonly RotationHostedService _rotationService;
    private readonly StartupManager _startupManager;
    private readonly Action _requestExit;
    private readonly ToolStripMenuItem _startupItem;
    private readonly HotCornerWatcher _hotCornerWatcher;
    private FlyoutWindow? _flyout;

    public TrayIconManager(SpotlightEngine engine, RotationHostedService rotationService, StartupManager startupManager, Action requestExit)
    {
        _engine = engine;
        _rotationService = rotationService;
        _startupManager = startupManager;
        _requestExit = requestExit;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Image suivante", null, async (_, _) => await OnNextAsync());
        menu.Items.Add("J'aime", null, async (_, _) => await OnLikeAsync());
        menu.Items.Add("Je n'aime pas", null, async (_, _) => await OnDislikeAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Ouvrir le dossier des images", null, (_, _) => OpenImagesFolder());

        _startupItem = new ToolStripMenuItem("Lancer au demarrage de Windows")
        {
            CheckOnClick = true,
            Checked = _startupManager.IsEnabled()
        };
        _startupItem.CheckedChanged += (_, _) => _startupManager.SetEnabled(_startupItem.Checked);
        menu.Items.Add(_startupItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => _requestExit());

        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Windows Spotlight Desktop",
            ContextMenuStrip = menu,
            Visible = true
        };

        // Le flyout n'apparait que lorsque la souris survole l'angle superieur droit
        // de l'ecran principal (pas de popup automatique, pas de clic sur l'icone).
        _hotCornerWatcher = new HotCornerWatcher();
        _hotCornerWatcher.CornerEntered += () => ShowFlyoutAutoHide();
    }

    private async Task OnNextAsync()
    {
        await _engine.ShowNextAsync();
        _rotationService.ResetTimer();
        if (_flyout is not null) await _flyout.RefreshAsync();
    }

    private async Task OnSelectAsync(string hash)
    {
        await _engine.ShowImageAsync(hash);
        _rotationService.ResetTimer();
        if (_flyout is not null) await _flyout.RefreshAsync();
    }

    private async Task OnLikeAsync()
    {
        await _engine.LikeCurrentAsync();
        if (_flyout is not null) await _flyout.RefreshAsync();
    }

    private async Task OnDislikeAsync()
    {
        await _engine.DislikeCurrentAsync();
        _rotationService.ResetTimer();
        if (_flyout is not null) await _flyout.RefreshAsync();
    }

    private static void OpenImagesFolder()
    {
        System.Diagnostics.Process.Start("explorer.exe", AppPaths.ImagesFolder);
    }

    private void ShowFlyoutAutoHide()
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        dispatcher?.InvokeAsync(async () =>
        {
            _flyout ??= new FlyoutWindow(_engine, OnSelectAsync, OnLikeAsync, OnDislikeAsync);
            await _flyout.ShowNearCursorAsync();
        });
    }

    public void Dispose()
    {
        _hotCornerWatcher.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
