using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PixivTools.Models;

namespace PixivTools.Services;

public class PixivApiService : IDisposable
{
    private readonly ConfigService _config;
    private readonly ILogger<PixivApiService> _logger;
    private readonly HttpClientHandler _handler;
    private readonly HttpClient _client;

    private const string ChromeUA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36 Edg/143.0.0.0";
    private const string MirrorDomain = "i.muxmus.com";
    private const string OriginalDomain = "i.pximg.net";

    public PixivApiService(ConfigService config, ILogger<PixivApiService> logger)
    {
        _config = config;
        _logger = logger;
        _handler = new HttpClientHandler { AllowAutoRedirect = true, AutomaticDecompression = DecompressionMethods.All };
        _client = new HttpClient(_handler) { Timeout = TimeSpan.FromSeconds(60) };
        UpdateProxy();
    }

    public void UpdateProxy()
    {
        var cfg = _config;
        if (cfg.ProxyEnabled && !string.IsNullOrWhiteSpace(cfg.ProxyAddr))
        {
            var url = $"{cfg.ProxyType}://{cfg.ProxyAddr}";
            _handler.Proxy = new WebProxy(url);
            _handler.UseProxy = true;
        }
        else
        {
            foreach (var key in new[] { "PIXIVTOOLS_PROXY", "ALL_PROXY", "HTTPS_PROXY", "HTTP_PROXY" })
            {
                var env = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrWhiteSpace(env))
                {
                    var url = env.Contains("://") ? env : "http://" + env;
                    _handler.Proxy = new WebProxy(url);
                    _handler.UseProxy = true;
                    return;
                }
            }
            _handler.Proxy = null;
            _handler.UseProxy = false;
        }
    }

    public string ApplyMirror(string url) => _config.Mirror && url.Contains(OriginalDomain) ? url.Replace(OriginalDomain, MirrorDomain) : url;

    public async Task<List<string>> FetchImageUrlsAsync(string pid)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"https://www.pixiv.net/ajax/illust/{pid}/pages?lang=zh");
            req.Headers.Add("Host", "www.pixiv.net");
            req.Headers.Add("User-Agent", ChromeUA);
            req.Headers.Add("Accept", "application/json");
            var cookie = _config.Cookie;
            if (!string.IsNullOrWhiteSpace(cookie)) req.Headers.Add("Cookie", cookie);

            var resp = await _client.SendAsync(req);
            if (resp.StatusCode != HttpStatusCode.OK) return new();
            var content = await resp.Content.ReadAsStringAsync();

            PixivPagesResponse? data;
            try { data = JsonSerializer.Deserialize<PixivPagesResponse>(content); }
            catch { content = content.Replace("'", "\""); try { data = JsonSerializer.Deserialize<PixivPagesResponse>(content); } catch { data = null; } }

            return data?.Body?.Where(x => x?.Urls?.Original != null).Select(x => x!.Urls!.Original!).ToList() ?? new();
        }
        catch (Exception ex) { _logger.LogError(ex, "获取图片链接异常 PID={Pid}", pid); return new(); }
    }

    public async Task<HttpResponseMessage?> DownloadImageAsync(string url)
    {
        try
        {
            url = ApplyMirror(url);
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("User-Agent", ChromeUA);
            if (url.Contains(OriginalDomain) || url.Contains(MirrorDomain))
            {
                req.Headers.Add("Host", "i.pximg.net");
                req.Headers.Add("Origin", "https://www.pixiv.net");
                req.Headers.Add("Referer", "https://www.pixiv.net");
            }
            return await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        }
        catch (Exception ex) { _logger.LogError(ex, "下载图片异常 Url={Url}", url); return null; }
    }

    public async Task<(int Success, int Fail)> DownloadUrlsAsync(List<string> urls, string destDir, string pid = "")
    {
        var suc = 0; var fail = 0;
        var subDir = string.IsNullOrEmpty(pid) ? destDir : Path.Combine(destDir, pid);
        foreach (var url in urls)
        {
            using var resp = await DownloadImageAsync(url);
            if (resp == null || resp.StatusCode != HttpStatusCode.OK) { fail++; continue; }
            var ct = resp.Content.Headers.ContentType?.MediaType ?? "";
            if (!ct.StartsWith("image/")) { fail++; continue; }
            try
            {
                var uri = new Uri(url);
                var name = Path.GetFileName(uri.AbsolutePath);
                if (string.IsNullOrEmpty(name) || name == "/") name = $"{Guid.NewGuid()}.jpg";
                var path = Path.Combine(subDir, name);
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(subDir);
                    var bytes = await resp.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(path, bytes);
                }
                suc++;
            }
            catch { fail++; }
        }
        return (suc, fail);
    }

    public async Task<RandomPicResponse?> FetchRandomPicAsync(bool nsfw, string keyword, string[] tags)
    {
        try
        {
            var searchKeyword = keyword ?? "";
            if (tags.Length > 0)
            {
                var tagPart = string.Join(" ", tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()));
                if (!string.IsNullOrWhiteSpace(searchKeyword))
                    searchKeyword += " " + tagPart;
                else
                    searchKeyword = tagPart;
            }

            if (string.IsNullOrWhiteSpace(searchKeyword))
            {
                _logger.LogWarning("随机图: 关键词和标签均为空");
                return null;
            }

            var mode = nsfw ? "r18" : "safe";
            var randomPage = Random.Shared.Next(1, 50);
            var searchUrl = $"https://www.pixiv.net/ajax/search/artworks/{Uri.EscapeDataString(searchKeyword)}?order=date_d&mode={mode}&p={randomPage}&ai_type=0&csw=0&s_mode=s_tag&lang=zh";

            _logger.LogInformation("随机图搜索: keyword={Keyword}, page={Page}, mode={Mode}", searchKeyword, randomPage, mode);

            var req = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            req.Headers.Add("Host", "www.pixiv.net");
            req.Headers.Add("User-Agent", ChromeUA);
            req.Headers.Add("Accept", "application/json");
            var cookie = _config.Cookie;
            if (!string.IsNullOrWhiteSpace(cookie)) req.Headers.Add("Cookie", cookie);

            var resp = await _client.SendAsync(req);
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning("搜索API返回非200: {Status}", resp.StatusCode);
                return null;
            }

            var content = await resp.Content.ReadAsStringAsync();
            SearchResponse? searchData;
            try { searchData = JsonSerializer.Deserialize<SearchResponse>(content); }
            catch { content = content.Replace("'", "\""); try { searchData = JsonSerializer.Deserialize<SearchResponse>(content); } catch { searchData = null; } }

            if (searchData?.Body?.IllustManga?.Data == null || searchData.Body.IllustManga.Data.Count == 0)
            {
                _logger.LogWarning("搜索结果为空");
                return new RandomPicResponse { Success = false, Message = "没有找到相关作品" };
            }

            var artworks = searchData.Body.IllustManga.Data;
            var randomIndex = Random.Shared.Next(0, artworks.Count);
            var selected = artworks[randomIndex];

            _logger.LogInformation("随机选中作品: {Id} - {Title}", selected.Id, selected.Title);

            var detailUrl = $"https://www.pixiv.net/ajax/illust/{selected.Id}?lang=zh";
            var detailReq = new HttpRequestMessage(HttpMethod.Get, detailUrl);
            detailReq.Headers.Add("Host", "www.pixiv.net");
            detailReq.Headers.Add("User-Agent", ChromeUA);
            detailReq.Headers.Add("Accept", "application/json");
            if (!string.IsNullOrWhiteSpace(cookie)) detailReq.Headers.Add("Cookie", cookie);

            var detailResp = await _client.SendAsync(detailReq);
            if (detailResp.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning("作品详情API返回非200: {Status}", detailResp.StatusCode);
                return null;
            }

            var detailContent = await detailResp.Content.ReadAsStringAsync();
            IllustDetailResponse? detailData;
            try { detailData = JsonSerializer.Deserialize<IllustDetailResponse>(detailContent); }
            catch { detailContent = detailContent.Replace("'", "\""); try { detailData = JsonSerializer.Deserialize<IllustDetailResponse>(detailContent); } catch { detailData = null; } }

            if (detailData?.Body == null)
            {
                _logger.LogWarning("作品详情解析失败");
                return null;
            }

            var illust = detailData.Body;
            var imageUrl = illust.Urls?.Original ?? illust.Urls?.Regular ?? "";

            var result = new RandomPicResponse
            {
                Success = true,
                Data = new List<RandomPicData>
                {
                    new RandomPicData
                    {
                        Pid = long.TryParse(selected.Id, out var pid) ? pid : 0,
                        Title = selected.Title ?? illust.IllustTitle ?? "",
                        Author = selected.UserName ?? illust.UserName ?? "",
                        Tags = selected.Tags ?? illust.Tags?.Tags?.Select(t => t.Tag ?? "").ToList() ?? new List<string>(),
                        UploadDate = DateTimeOffset.TryParse(illust.UploadDate, out var dt) ? dt.ToUnixTimeMilliseconds() : 0,
                        Url = imageUrl
                    }
                }
            };

            _logger.LogInformation("随机图获取成功: PID={Pid}, Title={Title}", result.Data[0].Pid, result.Data[0].Title);
            return result;
        }
        catch (HttpRequestException ex) { _logger.LogError(ex, "随机图网络错误"); throw; }
    }

    public async Task<List<string>> FetchUserArtworksAsync(string userId)
    {
        try
        {
            var url = $"https://www.pixiv.net/ajax/user/{userId}/profile/all?lang=zh";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("Host", "www.pixiv.net");
            req.Headers.Add("User-Agent", ChromeUA);
            req.Headers.Add("Accept", "application/json");
            var cookie = _config.Cookie;
            if (!string.IsNullOrWhiteSpace(cookie)) req.Headers.Add("Cookie", cookie);

            var resp = await _client.SendAsync(req);
            if (resp.StatusCode != HttpStatusCode.OK) return new();
            var content = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty("body", out var body)) return new();
            if (!body.TryGetProperty("illusts", out var illusts)) return new();

            var pids = new List<string>();
            foreach (var prop in illusts.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Null) continue;
                pids.Add(prop.Name);
            }
            _logger.LogInformation("用户 {UserId} 共 {Count} 个作品", userId, pids.Count);
            return pids;
        }
        catch (Exception ex) { _logger.LogError(ex, "获取用户作品失败"); return new(); }
    }

    public void Dispose() { _client?.Dispose(); _handler?.Dispose(); }
}
