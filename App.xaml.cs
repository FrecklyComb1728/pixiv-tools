using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PixivTools.Services;
using PixivTools.ViewModels;
using PixivTools.Views;

namespace PixivTools;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        var services = new ServiceCollection();

        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton<ConfigService>();
        services.AddSingleton<PixivApiService>();
        services.AddSingleton<ImageCacheService>();
        services.AddSingleton<ArtworkDatabase>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<PidSearchViewModel>();
        services.AddSingleton<RandomPicViewModel>();
        services.AddSingleton<BatchDownloadViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<GalleryViewModel>();
        services.AddTransient<PidSearchTab>();
        services.AddTransient<RandomPicTab>();
        services.AddTransient<BatchDownloadTab>();
        services.AddTransient<SettingsTab>();
        services.AddTransient<GalleryTab>();
        services.AddTransient<MainWindow>();

        var sp = services.BuildServiceProvider();
        var cfg = sp.GetRequiredService<ConfigService>();

        var win = sp.GetRequiredService<MainWindow>();
        win.Closed += (_, _) => cfg.SaveConfig();
        win.Show();
    }
}
