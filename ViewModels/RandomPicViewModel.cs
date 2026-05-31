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

public partial class RandomPicViewModel : ObservableObject
{
    private readonly PixivApiService _api;
    private readonly ConfigService _config;
    private readonly ILogger<RandomPicViewModel> _logger;

    public RandomPicViewModel(PixivApiService api, ConfigService config, ILogger<RandomPicViewModel> logger)
    {
        _api = api; _config = config; _logger = logger;
    }

    [ObservableProperty] private string _keyword = "";
    [ObservableProperty] private string _tagInput = "";
    [ObservableProperty] private bool _isNsfw;
    [ObservableProperty] private string _displayPid = "";
    [ObservableProperty] private string _displayTitle = "";
    [ObservableProperty] private string _displayAuthor = "";
    [ObservableProperty] private string _displayTags = "";
    [ObservableProperty] private string _displayDate = "";
    [ObservableProperty] private bool _isAiGenerated;
    [ObservableProperty] private string _statusText = "预览图将在这里展示...";
    [ObservableProperty] private object? _imageSource;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasImage;
    [ObservableProperty] private RandomPicResponse? _cachedInfo;
    [ObservableProperty] private byte[]? _currentImageData;
    [ObservableProperty] private string? _currentImagePath;

    [RelayCommand]
    private async Task RandomAsync()
    {
        IsLoading = true; HasImage = false; StatusText = "加载中，请稍候...";
        try
        {
            var tags = string.IsNullOrWhiteSpace(TagInput) ? Array.Empty<string>() : TagInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var response = await _api.FetchRandomPicAsync(IsNsfw, Keyword, tags);
            if (response == null) { StatusText = "获取随机图失败"; return; }
            if (!response.Success || response.Data == null || response.Data.Count == 0)
            { StatusText = response.Data == null ? "没有图片信息" : (response.Message ?? "没有图片信息"); return; }

            CachedInfo = response;
            var item = response.Data[0];
            DisplayPid = item.Pid.ToString(); DisplayTitle = item.Title ?? ""; DisplayAuthor = item.Author ?? "";
            if (item.Tags != null) { DisplayTags = string.Join(", ", item.Tags); IsAiGenerated = item.Tags.Any(t => t.Contains("ai绘图", StringComparison.OrdinalIgnoreCase)); }
            if (item.UploadDate > 0) { try { DisplayDate = DateTimeOffset.FromUnixTimeMilliseconds(item.UploadDate).ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss"); } catch { DisplayDate = ""; } }
            await DownloadAndShowAsync(item);
        }
        catch (HttpRequestException) { StatusText = "网络连接失败"; }
        catch (Exception ex) { _logger.LogError(ex, "随机图异常"); StatusText = "发生未知错误"; }
        finally { IsLoading = false; }
    }

    private async Task DownloadAndShowAsync(RandomPicData item)
    {
        if (string.IsNullOrWhiteSpace(item.Url)) return;
        var resp = await _api.DownloadImageAsync(item.Url);
        if (resp == null) { StatusText = "下载失败"; return; }
        var ct = resp.Content.Headers.ContentType?.MediaType ?? "";
        if (!ct.StartsWith("image/")) { StatusText = "图片加载失败"; resp.Dispose(); return; }
        var data = await resp.Content.ReadAsByteArrayAsync(); resp.Dispose();
        CurrentImageData = data;
        var path = ImageService.GetTempPath("r"); await File.WriteAllBytesAsync(path, data); CurrentImagePath = path;
        var img = ImageService.LoadImage(data);
        if (img != null) { ImageSource = img; HasImage = true; } else StatusText = "图片加载失败";
    }

    [RelayCommand] private void SavePictureR()
    {
        if (CurrentImageData == null) return;
        var dialog = new SaveFileDialog { Filter = "PNG图片|*.png", FileName = $"{DisplayPid}.png" };
        if (dialog.ShowDialog() == true)
            try { File.WriteAllBytes(dialog.FileName, CurrentImageData); SnackbarHelper.Show("保存成功", dialog.FileName); }
            catch (Exception ex) { _logger.LogError(ex, "保存失败"); SnackbarHelper.Show("错误", "保存失败", "Error"); }
    }

    [RelayCommand] private async Task ReloadPictureRAsync()
    { if (CachedInfo?.Data is { Count: > 0 }) await DownloadAndShowAsync(CachedInfo.Data[0]); }

    [RelayCommand] private void ResetPictureR()
    { StatusText = "预览图将在这里展示..."; ImageSource = null; HasImage = false; CachedInfo = null; CurrentImageData = null; CurrentImagePath = null; DisplayPid = ""; DisplayTitle = ""; DisplayAuthor = ""; DisplayTags = ""; DisplayDate = ""; IsAiGenerated = false; }

    [RelayCommand] private void OpenPictureR()
    { if (!string.IsNullOrWhiteSpace(CurrentImagePath) && File.Exists(CurrentImagePath)) Process.Start(new ProcessStartInfo(CurrentImagePath) { UseShellExecute = true }); }
}
