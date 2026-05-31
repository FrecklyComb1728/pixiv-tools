using System.Diagnostics;
using System.IO;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PixivTools.Models;
using PixivTools.Services;

namespace PixivTools.ViewModels;

public partial class PidSearchViewModel : ObservableObject
{
    private readonly PixivApiService _api;
    private readonly ConfigService _config;
    private readonly ImageCacheService _cache;
    private readonly ArtworkDatabase _db;
    private readonly ILogger<PidSearchViewModel> _logger;

    public PidSearchViewModel(PixivApiService api, ConfigService config, ImageCacheService cache, ArtworkDatabase db, ILogger<PidSearchViewModel> logger)
    {
        _api = api; _config = config; _cache = cache; _db = db; _logger = logger;
    }

    [ObservableProperty] private string _pid = "";
    [ObservableProperty] private bool _isMultiPage;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _statusText = "预览图将在这里展示...";
    [ObservableProperty] private object? _imageSource;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasImage;
    [ObservableProperty] private string? _currentImageUrl;
    [ObservableProperty] private byte[]? _currentImageData;
    [ObservableProperty] private string? _currentImagePath;

    private List<string> _allUrls = new();

    public string PageIndicator => $"{Page}/{TotalPages}";
    public bool HasPrevPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Pid)) { StatusText = "请输入PID"; return; }
        if (!Pid.All(char.IsDigit)) { StatusText = "PID必须为数字"; return; }
        IsLoading = true; HasImage = false; StatusText = "加载中，请稍候...";

        try
        {
            _allUrls = await _api.FetchImageUrlsAsync(Pid);
            if (_allUrls.Count == 0) { StatusText = "获取图片链接失败"; return; }

            TotalPages = _allUrls.Count;
            Page = IsMultiPage ? Math.Clamp(Page, 1, TotalPages) : 1;
            OnPropertyChanged(nameof(PageIndicator));
            OnPropertyChanged(nameof(HasPrevPage));
            OnPropertyChanged(nameof(HasNextPage));

            await LoadPageAsync();
        }
        catch (HttpRequestException) { StatusText = "网络连接失败"; }
        catch (Exception ex) { _logger.LogError(ex, "PID查图异常"); StatusText = "发生未知错误"; }
        finally { IsLoading = false; }
    }

    private async Task LoadPageAsync()
    {
        if (_allUrls.Count == 0) return;
        var idx = Math.Clamp(Page - 1, 0, _allUrls.Count - 1);
        var selectedUrl = _api.ApplyMirror(_allUrls[idx]);
        CurrentImageUrl = selectedUrl;
        await LoadAndShowImageAsync(selectedUrl);

        var tempDir = Path.Combine(Path.GetTempPath(), "PixivTools");
        Directory.CreateDirectory(tempDir);
        _ = Task.Run(async () => { try { await _api.DownloadUrlsAsync(_allUrls, tempDir, Pid); } catch { } });

        if (_config.Remember) HistoryService.Append(Pid, IsMultiPage, Page);

        _ = Task.Run(() => _cache.PreDownloadAsync(_api, _allUrls, idx, _config.PreDownloadCount));
    }

    [RelayCommand]
    private void PrevPage()
    {
        if (Page > 1) { Page--; OnPropertyChanged(nameof(PageIndicator)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage)); _ = LoadPageAsync(); }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (Page < TotalPages) { Page++; OnPropertyChanged(nameof(PageIndicator)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage)); _ = LoadPageAsync(); }
    }

    private async Task LoadAndShowImageAsync(string url)
    {
        try
        {
            var cached = _cache.Get(url);
            byte[] data;
            string? localPath = null;

            if (cached != null)
            {
                data = cached;
                _logger.LogInformation("从缓存加载图片");
            }
            else
            {
                var resp = await _api.DownloadImageAsync(url);
                if (resp == null) { StatusText = "连接失败"; return; }
                var statusMsg = CheckHttpStatus((int)resp.StatusCode);
                if (statusMsg != null) { StatusText = statusMsg; resp.Dispose(); return; }
                var ct = resp.Content.Headers.ContentType?.MediaType ?? "";
                if (!ct.StartsWith("image/")) { StatusText = "图片加载失败"; resp.Dispose(); return; }

                data = await resp.Content.ReadAsByteArrayAsync(); resp.Dispose();
                if (data.Length == 0) { StatusText = "图片加载失败"; return; }

                _cache.Put(url, data);
            }

            var pidDir = Path.Combine(Path.GetTempPath(), "PixivTools", Pid);
            Directory.CreateDirectory(pidDir);
            var uri = new Uri(url); var name = Path.GetFileName(uri.AbsolutePath);
            localPath = Path.Combine(pidDir, name);
            if (!File.Exists(localPath)) await File.WriteAllBytesAsync(localPath, data);

            CurrentImageData = data; CurrentImagePath = localPath;

            var img = ImageService.LoadImage(data);
            if (img != null) { ImageSource = img; HasImage = true; StatusText = $"PID: {Pid}  第 {Page}/{TotalPages} 页"; }
            else StatusText = "图片加载失败";
        }
        catch (HttpRequestException) { StatusText = "连接失败"; }
        catch (Exception ex) { _logger.LogError(ex, "加载图片异常"); StatusText = "图片加载失败"; }
    }

    private static string? CheckHttpStatus(int code) => code switch
    {
        404 => "图片不存在 (404)",
        403 => "服务器拒绝访问 (403)",
        504 => "网关超时 (504)",
        408 => "请求超时 (408)",
        503 => "服务不可用 (503)",
        >= 300 and < 400 => "重定向错误",
        _ => null
    };

    [RelayCommand] private void SavePicture()
    {
        if (CurrentImageData == null) return;
        var ext = Path.GetExtension(CurrentImagePath ?? ".jpg").TrimStart('.');
        var dest = ImageService.GetDefaultDestDir(_config.FolderName, _config.AutoFolder);
        Directory.CreateDirectory(dest);
        var dialog = new SaveFileDialog { Filter = $"图片|*.{ext}", FileName = $"{Pid}_p{Page}.{ext}", InitialDirectory = dest };
        if (dialog.ShowDialog() == true)
            try
            {
                File.WriteAllBytes(dialog.FileName, CurrentImageData);
                _db.Insert(new ArtworkRecord { Pid = Pid, FilePath = dialog.FileName, FileSize = CurrentImageData.Length });
                SnackbarHelper.Show("保存成功", dialog.FileName);
            }
            catch (Exception ex) { _logger.LogError(ex, "保存失败"); SnackbarHelper.Show("错误", "保存失败", "Error"); }
    }

    [RelayCommand] private async Task ReloadPictureAsync()
    { if (!string.IsNullOrWhiteSpace(CurrentImageUrl)) await LoadAndShowImageAsync(CurrentImageUrl); }

    [RelayCommand] private void ResetPicture()
    {
        StatusText = "预览图将在这里展示..."; ImageSource = null; HasImage = false;
        CurrentImageUrl = null; CurrentImageData = null; CurrentImagePath = null;
        _allUrls.Clear(); TotalPages = 0; Page = 1;
        OnPropertyChanged(nameof(PageIndicator)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage));
    }

    [RelayCommand] private void OpenPicture()
    { if (!string.IsNullOrWhiteSpace(CurrentImagePath) && File.Exists(CurrentImagePath)) Process.Start(new ProcessStartInfo(CurrentImagePath) { UseShellExecute = true }); }
}
