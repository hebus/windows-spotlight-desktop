using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SpotlightDesktop.Models;
using SpotlightDesktop.Services;

namespace SpotlightDesktop.UI;

public sealed class FlyoutImageItem
{
    public required string Hash { get; init; }
    public BitmapImage? Thumbnail { get; init; }
    public bool IsActive { get; init; }
}

public sealed class FlyoutViewModel : INotifyPropertyChanged
{
    private const int PreviewCount = 3;
    private const int ThumbnailDecodeWidth = 160;

    private readonly SpotlightEngine _engine;
    private string _title = "";
    private string _description = "";
    private string _copyright = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FlyoutImageItem> Items { get; } = new();

    public FlyoutViewModel(SpotlightEngine engine, Func<string, Task> onSelect, Func<Task> onLike, Func<Task> onDislike)
    {
        _engine = engine;
        SelectCommand = new RelayCommand<string>(onSelect);
        LikeCommand = new RelayCommand(onLike);
        DislikeCommand = new RelayCommand(onDislike);
    }

    public string Title { get => _title; private set { _title = value; OnChanged(); } }
    public string Description { get => _description; private set { _description = value; OnChanged(); } }
    public string Copyright { get => _copyright; private set { _copyright = value; OnChanged(); } }

    public ICommand SelectCommand { get; }
    public ICommand LikeCommand { get; }
    public ICommand DislikeCommand { get; }

    public async Task RefreshFromEngineAsync()
    {
        await _engine.EnsureUpcomingPoolAsync(PreviewCount);

        var current = _engine.CurrentImage;
        if (current is null) return;

        Title = current.Title;
        Description = current.Description;
        Copyright = current.Copyright;

        Items.Clear();
        Items.Add(BuildItem(current, isActive: true));
        foreach (var upcoming in _engine.SelectUpcoming(PreviewCount))
            Items.Add(BuildItem(upcoming, isActive: false));
    }

    private static FlyoutImageItem BuildItem(SpotlightImage image, bool isActive)
    {
        var path = Path.Combine(AppPaths.ImagesFolder, image.FileName);
        BitmapImage? bitmap = null;
        if (File.Exists(path))
        {
            bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = ThumbnailDecodeWidth;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
        }

        return new FlyoutImageItem { Hash = image.Hash, Thumbnail = bitmap, IsActive = isActive };
    }

    private void OnChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

internal sealed class RelayCommand : ICommand
{
    private readonly Func<Task> _execute;

    public RelayCommand(Func<Task> execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public async void Execute(object? parameter) => await _execute();
}

internal sealed class RelayCommand<T> : ICommand
{
    private readonly Func<T, Task> _execute;

    public RelayCommand(Func<T, Task> execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public async void Execute(object? parameter)
    {
        if (parameter is T typed) await _execute(typed);
    }
}
