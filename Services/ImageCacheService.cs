using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PixivTools.Services;

public class ImageCacheService
{
    private readonly ILogger<ImageCacheService> _logger;
    private readonly string _cacheDir;

    public ImageCacheService(ILogger<ImageCacheService> logger)
    {
        _logger = logger;
        _cacheDir = Path.Combine(Path.GetTempPath(), "PixivTools", "image_cache");
        Directory.CreateDirectory(_cacheDir);
        _logger.LogInformation("图片缓存目录: {Dir}", _cacheDir);
    }

    private string GetCacheKey(string url)
    {
        var hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(url)));
        var ext = Path.GetExtension(new Uri(url).AbsolutePath);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        return Path.Combine(_cacheDir, hash + ext);
    }

    public byte[]? Get(string url)
    {
        var path = GetCacheKey(url);
        if (!File.Exists(path)) return null;
        try
        {
            var data = File.ReadAllBytes(path);
            _logger.LogDebug("缓存命中: {Url}", url);
            return data;
        }
        catch { return null; }
    }

    public void Put(string url, byte[] data)
    {
        try
        {
            var path = GetCacheKey(url);
            File.WriteAllBytes(path, data);
            _logger.LogDebug("已缓存: {Url}", url);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "缓存写入失败"); }
    }

    public bool Exists(string url) => File.Exists(GetCacheKey(url));

    public async Task PreDownloadAsync(PixivApiService api, List<string> urls, int currentIndex, int count = 3)
    {
        var tasks = new List<Task>();
        for (var i = currentIndex + 1; i <= Math.Min(currentIndex + count, urls.Count - 1); i++)
        {
            var url = api.ApplyMirror(urls[i]);
            if (Exists(url)) continue;
            tasks.Add(DownloadAndCacheAsync(api, url));
        }
        if (tasks.Count > 0)
        {
            _logger.LogInformation("预下载 {Count} 张图片", tasks.Count);
            await Task.WhenAll(tasks);
        }
    }

    private async Task DownloadAndCacheAsync(PixivApiService api, string url)
    {
        try
        {
            var resp = await api.DownloadImageAsync(url);
            if (resp == null) return;
            var ct = resp.Content.Headers.ContentType?.MediaType ?? "";
            if (!ct.StartsWith("image/")) { resp.Dispose(); return; }
            var data = await resp.Content.ReadAsByteArrayAsync();
            resp.Dispose();
            if (data.Length > 0) Put(url, data);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "预下载失败: {Url}", url); }
    }

    public void Clear()
    {
        try
        {
            foreach (var file in Directory.GetFiles(_cacheDir))
                try { File.Delete(file); } catch { }
            _logger.LogInformation("缓存已清空");
        }
        catch { }
    }
}
