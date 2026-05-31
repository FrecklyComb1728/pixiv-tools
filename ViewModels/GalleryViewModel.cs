using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixivTools.Models;
using PixivTools.Services;

namespace PixivTools.ViewModels;

public partial class GalleryViewModel : ObservableObject
{
    private readonly ArtworkDatabase _db;
    private static readonly SemaphoreSlim ThumbnailSemaphore = new(4);

    public GalleryViewModel(ArtworkDatabase db) { _db = db; }

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _searchType = "综合";
    [ObservableProperty] private string _filterAuthor = "";
    [ObservableProperty] private string _sortBy = "时间 ↓";
    [ObservableProperty] private int _pageIndex;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _statusText = "就绪";
    [ObservableProperty] private ObservableCollection<ArtworkRecord> _items = new();

    [ObservableProperty] private bool _isPreviewOpen;
    [ObservableProperty] private string _previewTitle = "";
    [ObservableProperty] private ObservableCollection<object> _previewImages = new();
    [ObservableProperty] private int _previewIndex;
    [ObservableProperty] private object? _previewCurrent;
    public string PreviewIndicator => $"{PreviewIndex + 1}/{PreviewImages.Count}";
    public bool HasPrevPreview => PreviewIndex > 0;
    public bool HasNextPreview => PreviewIndex < PreviewImages.Count - 1;

    public string[] SearchTypes { get; } = { "综合", "标题", "作者", "PID", "标签" };
    public string[] SortOptions { get; } = { "时间 ↓", "时间 ↑", "图片数 ↓" };

    private const int PageSize = 24;
    public int TotalPages => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
    public string PageInfo => $"第 {PageIndex + 1}/{TotalPages} 页 · 共 {TotalCount} 条";

    private string ResolveSort() => SortBy switch { "时间 ↑" => "date-asc", "图片数 ↓" => "pages-desc", _ => "date-desc" };

    [RelayCommand]
    private void Load()
    {
        var sort = ResolveSort();
        var searchKeyword = SearchType switch
        {
            "标题" => $"t:{SearchText}", "作者" => $"a:{SearchText}",
            "PID" => SearchText, "标签" => $"g:{SearchText}", _ => SearchText
        };

        TotalCount = _db.Count(searchKeyword, null, SearchType == "作者" ? null : FilterAuthor);
        PageIndex = Math.Clamp(PageIndex, 0, Math.Max(0, TotalPages - 1));
        var results = _db.Query(searchKeyword, null, SearchType == "作者" ? null : FilterAuthor, sort, PageSize, PageIndex * PageSize);

        Items = new ObservableCollection<ArtworkRecord>(results);
        StatusText = $"共 {TotalCount} 条";
        foreach (var item in Items) _ = LoadThumbAsync(item);
    }

    private static async Task LoadThumbAsync(ArtworkRecord r)
    {
        try
        {
            var dir = r.FilePath;
            if (string.IsNullOrWhiteSpace(dir)) return;
            if (File.Exists(dir)) dir = Path.GetDirectoryName(dir) ?? "";
            if (!Directory.Exists(dir)) return;
            var first = Directory.GetFiles(dir).OrderBy(f => f).FirstOrDefault();
            if (first == null) return;

            await ThumbnailSemaphore.WaitAsync();
            try
            {
                var data = await File.ReadAllBytesAsync(first);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = new MemoryStream(data);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 200;
                bmp.EndInit();
                bmp.Freeze();
                r.Thumbnail = bmp;
            }
            finally { ThumbnailSemaphore.Release(); }
        }
        catch { }
    }

    [RelayCommand] private void PrevPage() { if (PageIndex > 0) { PageIndex--; Load(); } }
    [RelayCommand] private void NextPage() { if (PageIndex < TotalPages - 1) { PageIndex++; Load(); } }
    [RelayCommand] private void Search() { PageIndex = 0; Load(); }

    [RelayCommand]
    private void OpenPid(string pid)
    {
        var item = Items.FirstOrDefault(i => i.Pid == pid);
        if (item == null) return;
        try
        {
            var dir = item.FilePath;
            if (string.IsNullOrWhiteSpace(dir)) return;
            if (File.Exists(dir)) dir = Path.GetDirectoryName(dir) ?? "";
            if (!Directory.Exists(dir)) return;

            var files = Directory.GetFiles(dir).OrderBy(f => f).ToList();
            if (files.Count == 0) return;

            var images = new ObservableCollection<object>();
            foreach (var f in files)
            {
                var data = File.ReadAllBytes(f);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = new MemoryStream(data);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                images.Add(bmp);
            }

            PreviewImages = images;
            PreviewIndex = 0;
            PreviewCurrent = images[0];
            PreviewTitle = $"{pid}  {1}/{files.Count}";
            IsPreviewOpen = true;
            OnPropertyChanged(nameof(PreviewIndicator));
            OnPropertyChanged(nameof(HasPrevPreview));
            OnPropertyChanged(nameof(HasNextPreview));
        }
        catch { }
    }

    [RelayCommand] private void ClosePreview() { IsPreviewOpen = false; PreviewImages = new(); PreviewCurrent = null; }

    [RelayCommand]
    private void PrevPreview()
    {
        if (PreviewIndex <= 0) return;
        PreviewIndex--;
        PreviewCurrent = PreviewImages[PreviewIndex];
        PreviewTitle = PreviewTitle.Substring(0, PreviewTitle.LastIndexOf("  ") + 2) + $"{PreviewIndex + 1}/{PreviewImages.Count}";
        OnPropertyChanged(nameof(PreviewIndicator));
        OnPropertyChanged(nameof(HasPrevPreview));
        OnPropertyChanged(nameof(HasNextPreview));
    }

    [RelayCommand]
    private void NextPreview()
    {
        if (PreviewIndex >= PreviewImages.Count - 1) return;
        PreviewIndex++;
        PreviewCurrent = PreviewImages[PreviewIndex];
        PreviewTitle = PreviewTitle.Substring(0, PreviewTitle.LastIndexOf("  ") + 2) + $"{PreviewIndex + 1}/{PreviewImages.Count}";
        OnPropertyChanged(nameof(PreviewIndicator));
        OnPropertyChanged(nameof(HasPrevPreview));
        OnPropertyChanged(nameof(HasNextPreview));
    }

    [RelayCommand]
    private void Delete(string pid)
    {
        var item = Items.FirstOrDefault(i => i.Pid == pid);
        if (item == null) return;
        try
        {
            var dir = item.FilePath;
            if (string.IsNullOrWhiteSpace(dir)) return;
            if (File.Exists(dir)) dir = Path.GetDirectoryName(dir) ?? "";
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            _db.Delete(pid);
            Items.Remove(item);
            TotalCount--;
            StatusText = $"共 {TotalCount} 条";
        }
        catch (Exception ex) { StatusText = $"删除失败: {ex.Message}"; }
    }
}
