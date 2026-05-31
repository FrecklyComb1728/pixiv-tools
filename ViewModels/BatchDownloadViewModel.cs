using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PixivTools.Services;

namespace PixivTools.ViewModels;

public partial class DownloadItem : ObservableObject
{
    [ObservableProperty] private string _pid = "";
    [ObservableProperty] private string _status = "等待中";
    [ObservableProperty] private bool _isSelected = true;
}

public partial class BatchDownloadViewModel : ObservableObject
{
    private readonly PixivApiService _api;
    private readonly ConfigService _config;
    private readonly ArtworkDatabase _db;
    private readonly ILogger<BatchDownloadViewModel> _logger;

    public BatchDownloadViewModel(PixivApiService api, ConfigService config, ArtworkDatabase db, ILogger<BatchDownloadViewModel> logger)
    {
        _api = api; _config = config; _db = db; _logger = logger;
    }

    [ObservableProperty] private string _inputPid = "";
    [ObservableProperty] private string _userId = "";
    [ObservableProperty] private ObservableCollection<DownloadItem> _items = new();
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private string _statusText = "就绪";
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private string _downloadStats = "";
    [ObservableProperty] private DownloadItem? _selectedItem;
    private CancellationTokenSource? _cts;

    [RelayCommand] private void AddPid()
    {
        if (string.IsNullOrWhiteSpace(InputPid)) return;
        if (!InputPid.All(char.IsDigit)) { SnackbarHelper.Show("提示", "PID必须为数字", "Warning"); return; }
        if (Items.Any(i => i.Pid == InputPid)) { SnackbarHelper.Show("提示", $"PID {InputPid} 已存在", "Warning"); InputPid = ""; return; }
        Items.Add(new DownloadItem { Pid = InputPid }); InputPid = "";
    }

    [RelayCommand] private void DeleteSelected()
    { if (SelectedItem != null) { Items.Remove(SelectedItem); SelectedItem = Items.FirstOrDefault(); } }

    [RelayCommand] private void ClearAll() { Items.Clear(); DownloadStats = ""; }

    [RelayCommand]
    private async Task FetchUserAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId)) return;
        StatusText = $"正在获取用户 {UserId} 的作品...";
        try
        {
            var pids = await _api.FetchUserArtworksAsync(UserId);
            foreach (var pid in pids)
                if (!Items.Any(i => i.Pid == pid))
                    Items.Add(new DownloadItem { Pid = pid });
            StatusText = $"已添加 {pids.Count} 个作品";
        }
        catch { StatusText = "获取失败"; }
    }

    [RelayCommand]
    private async Task StartDownloadAsync()
    {
        var toDownload = Items.Where(i => i.IsSelected).ToList();
        if (toDownload.Count == 0) { SnackbarHelper.Show("提示", "没有可下载的项目", "Warning"); return; }
        IsDownloading = true; ProgressValue = 0;
        var successCount = 0; var failCount = 0;
        var dest = ImageService.GetDefaultDestDir(_config.FolderName, _config.AutoFolder);
        if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);
        _cts?.Dispose(); _cts = new CancellationTokenSource();
        var total = toDownload.Count;

        for (var i = 0; i < total; i++)
        {
            if (_cts.IsCancellationRequested) break;
            var item = toDownload[i]; item.Status = "下载中..."; StatusText = $"({i + 1}/{total}) 正在下载 PID: {item.Pid}";
            try
            {
                var urls = await _api.FetchImageUrlsAsync(item.Pid);
                if (urls.Count == 0) { item.Status = "失败"; failCount++; continue; }
                var (suc, _) = await _api.DownloadUrlsAsync(urls, dest, item.Pid);
                if (suc > 0) { item.Status = "成功"; successCount += suc;
                    var firstFile = Directory.Exists(Path.Combine(dest, item.Pid)) 
                        ? Directory.GetFiles(Path.Combine(dest, item.Pid)).FirstOrDefault() : null;
                    _db.Insert(new Models.ArtworkRecord { Pid = item.Pid, PageCount = urls.Count, FilePath = firstFile ?? Path.Combine(dest, item.Pid) }); }
                else { item.Status = "失败"; failCount++; }
            }
            catch (Exception ex) { _logger.LogError(ex, "下载PID {Pid} 异常", item.Pid); item.Status = "失败"; failCount++; }
            ProgressValue = 100 * (i + 1) / total;
            await Task.Delay(100);
        }

        if (_cts.IsCancellationRequested) StatusText = "已取消";
        else StatusText = "下载完成";
        DownloadStats = $"共 {total} 个PID，成功 {successCount} 次，失败 {failCount} 次";
        IsDownloading = false; _logger.LogInformation("批量下载结束: {Stats}", DownloadStats);
    }

    [RelayCommand] private void CancelDownload() { _cts?.Cancel(); StatusText = "正在取消..."; }
}
