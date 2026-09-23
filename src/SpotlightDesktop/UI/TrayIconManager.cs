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
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) ToggleFlyout();
        };

        _engine.CurrentImageChanged += _ => ShowFlyoutAutoHide();
    }

    private async Task OnNextAsync()
    {
        await _engine.ShowNextAsync();
        _rotationService.ResetTimer();
    }

    private async Task OnLikeAsync()
    {
        await _engine.LikeCurrentAsync();
        _flyout?.Refresh();
    }

    private async Task OnDislikeAsync()
    {
        await _engine.DislikeCurrentAsync();
        _rotationService.ResetTimer();
    }

    private static void OpenImagesFolder()
    {
        System.Diagnostics.Process.Start("explorer.exe", AppPaths.ImagesFolder);
    }

    private void ToggleFlyout()
    {
        if (_flyout is { IsVisible: true })
        {
            _flyout.Hide();
        }
        else
        {
            ShowFlyoutAutoHide();
        }
    }

    private void ShowFlyoutAutoHide()
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            _flyout ??= new FlyoutWindow(_engine, OnNextAsync, OnLikeAsync, OnDislikeAsync);
            _flyout.ShowNearCursor();
        });
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
