using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PixivTools.ViewModels;

namespace PixivTools.Views;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _sp;
    private readonly Dictionary<string, object> _pages = new();

    public MainWindow(MainViewModel mainVM, ILogger<MainWindow> logger, IServiceProvider sp)
    {
        InitializeComponent();
        _sp = sp;
        DataContext = mainVM;

        _pages["pidSearch"] = sp.GetRequiredService<PidSearchTab>();
        _pages["randomPic"] = sp.GetRequiredService<RandomPicTab>();
        _pages["batchDownload"] = sp.GetRequiredService<BatchDownloadTab>();
        _pages["settings"] = sp.GetRequiredService<SettingsTab>();
        _pages["about"] = new AboutTab();

        PageHost.Content = _pages["pidSearch"];

        Loaded += (_, _) =>
        {
            SnackbarHelper.ShowAction = (title, msg, _) =>
            {
                MessageBox.Show($"{title}: {msg}", "Pixiv工具箱", MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            };
        };

        Closed += (_, _) => Services.ImageService.CleanTempFiles();
    }

    private void OnNavSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lb || lb.SelectedIndex < 0) return;
        var tag = lb.SelectedIndex switch { 0 => "pidSearch", 1 => "randomPic", 2 => "batchDownload", 3 => "settings", 4 => "about", _ => null };
        if (tag != null && _pages.TryGetValue(tag, out var page))
            PageHost.Content = page;
    }
}
