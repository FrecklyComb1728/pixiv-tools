using System.IO;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PixivTools.Services;
using PixivTools.Views;

namespace PixivTools.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ConfigService _config;
    private readonly PixivApiService _api;
    private readonly ILogger<SettingsViewModel> _logger;

    public SettingsViewModel(ConfigService config, PixivApiService api, ILogger<SettingsViewModel> logger)
    {
        _config = config; _api = api; _logger = logger;
    }

    public string[] ProxyTypes { get; } = { "http", "socks4", "socks4a", "socks5", "socks5h" };

    public bool Mirror { get => _config.Mirror; set => _config.Mirror = value; }
    public bool Remember { get => _config.Remember; set => _config.Remember = value; }
    public bool AutoFolder { get => _config.AutoFolder; set => _config.AutoFolder = value; }
    public string FolderName { get => _config.FolderName; set => _config.FolderName = value; }
    public bool Nsfw { get => _config.Nsfw; set => _config.Nsfw = value; }
    public string PicType { get => _config.PicType; set => _config.PicType = value; }
    public string DlPicType { get => _config.DlPicType; set => _config.DlPicType = value; }
    public string Cookie { get => _config.Cookie; set => _config.Cookie = value; }
    public int PreDownloadCount { get => _config.PreDownloadCount; set => _config.PreDownloadCount = value; }

    public bool ProxyEnabled { get => _config.ProxyEnabled; set { _config.ProxyEnabled = value; _api.UpdateProxy(); } }
    public string ProxyType { get => _config.ProxyType; set { _config.ProxyType = value; _api.UpdateProxy(); } }
    public string ProxyAddr { get => _config.ProxyAddr; set { _config.ProxyAddr = value; _api.UpdateProxy(); } }

    [RelayCommand]
    private void Login()
    {
        var win = new LoginWindow(_config, _logger);
        win.Owner = System.Windows.Application.Current.MainWindow;
        win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        if (win.ShowDialog() == true) { OnPropertyChanged(nameof(Cookie)); SnackbarHelper.Show("登录成功", "Cookie已捕获并保存"); }
        else SnackbarHelper.Show("提示", "登录已取消或未检测到登录状态", "Warning");
    }

    [RelayCommand]
    private void OpenConfigDir()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_pixivtools_");
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
    }
}
