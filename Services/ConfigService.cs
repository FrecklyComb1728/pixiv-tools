using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PixivTools.Models;

namespace PixivTools.Services;

public partial class ConfigService : ObservableObject
{
    private readonly ILogger<ConfigService> _logger;
    private readonly AppConfig _config;

    public ConfigService(ILogger<ConfigService> logger)
    {
        _logger = logger;
        _config = AppConfig.Load();
        _logger.LogInformation("配置加载成功");
    }

    public void SaveConfig()
    {
        try { _config.Save(); _logger.LogDebug("配置已保存"); }
        catch (Exception ex) { _logger.LogError(ex, "配置保存失败"); }
    }

    public string Cookie { get => _config.Cookie; set { if (_config.Cookie != value) { _config.Cookie = value; OnPropertyChanged(); SaveConfig(); } } }
    public string Theme { get => _config.Theme; set { if (_config.Theme != value) { _config.Theme = value; OnPropertyChanged(); SaveConfig(); } } }
    public bool Mirror { get => _config.Mirror; set { _config.Mirror = value; OnPropertyChanged(); SaveConfig(); } }
    public bool Remember { get => _config.Remember; set { _config.Remember = value; OnPropertyChanged(); SaveConfig(); } }
    public string PicType { get => _config.PicType; set { _config.PicType = value; OnPropertyChanged(); SaveConfig(); } }
    public string DlPicType { get => _config.DlPicType; set { _config.DlPicType = value; OnPropertyChanged(); SaveConfig(); } }
    public bool AutoFolder { get => _config.AutoFolder; set { _config.AutoFolder = value; OnPropertyChanged(); SaveConfig(); } }
    public string FolderName { get => _config.FolderName; set { _config.FolderName = value; OnPropertyChanged(); SaveConfig(); } }
    public bool Nsfw { get => _config.Nsfw; set { _config.Nsfw = value; OnPropertyChanged(); SaveConfig(); } }
    public bool MultiPage { get => _config.MultiPage; set { _config.MultiPage = value; OnPropertyChanged(); SaveConfig(); } }
    public int Page { get => _config.Page; set { _config.Page = value; OnPropertyChanged(); SaveConfig(); } }
    public string ProxyType { get => _config.ProxyType; set { _config.ProxyType = value; OnPropertyChanged(); SaveConfig(); } }
    public string ProxyAddr { get => _config.ProxyAddr; set { _config.ProxyAddr = value; OnPropertyChanged(); SaveConfig(); } }
    public bool ProxyEnabled { get => _config.ProxyEnabled; set { _config.ProxyEnabled = value; OnPropertyChanged(); SaveConfig(); } }
    public int PreDownloadCount { get => _config.PreDownloadCount; set { _config.PreDownloadCount = value; OnPropertyChanged(); SaveConfig(); } }
}
