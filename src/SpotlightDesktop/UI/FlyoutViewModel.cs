using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SpotlightDesktop.Services;

namespace SpotlightDesktop.UI;

public sealed class FlyoutViewModel : INotifyPropertyChanged
{
    private readonly SpotlightEngine _engine;
    private string _title = "";
    private string _description = "";
    private string _copyright = "";
    private BitmapImage? _thumbnailSource;

    public event PropertyChangedEventHandler? PropertyChanged;

    public FlyoutViewModel(SpotlightEngine engine, Func<Task> onNext, Func<Task> onLike, Func<Task> onDislike)
    {
        _engine = engine;
        NextCommand = new RelayCommand(onNext);
        LikeCommand = new RelayCommand(onLike);
        DislikeCommand = new RelayCommand(onDislike);
        RefreshFromEngine();
    }

    public string Title { get => _title; private set { _title = value; OnChanged(); } }
    public string Description { get => _description; private set { _description = value; OnChanged(); } }
    public string Copyright { get => _copyright; private set { _copyright = value; OnChanged(); } }
    public BitmapImage? ThumbnailSource { get => _thumbnailSource; private set { _thumbnailSource = value; OnChanged(); } }

    public ICommand NextCommand { get; }
    public ICommand LikeCommand { get; }
    public ICommand DislikeCommand { get; }

    public void RefreshFromEngine()
    {
        var image = _engine.CurrentImage;
        if (image is null) return;

        Title = image.Title;
        Description = image.Description;
        Copyright = image.Copyright;

        var path = Path.Combine(AppPaths.ImagesFolder, image.FileName);
        if (File.Exists(path))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            ThumbnailSource = bitmap;
        }
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
