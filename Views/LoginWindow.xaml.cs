using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using PixivTools.Services;

namespace PixivTools.Views;

public partial class LoginWindow : Window
{
    private readonly ConfigService _config;
    private readonly ILogger _logger;
    private bool _captured;

    public LoginWindow(ConfigService config, ILogger logger)
    {
        InitializeComponent();
        _config = config; _logger = logger;
        WebView.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (e.IsSuccess) Dispatcher.Invoke(() => LoadingOverlay.Visibility = Visibility.Collapsed);
        };
        WebView.NavigationCompleted += OnNav;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try { await WebView.EnsureCoreWebView2Async(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebView2 初始化失败");
            StatusText.Text = "WebView2 初始化失败，请安装 Edge WebView2 Runtime";
            WaitRing.Visibility = Visibility.Collapsed;
        }
    }

    private async void OnNav(object? s, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_captured) return;
        try
        {
            var url = WebView.CoreWebView2.Source;
            var isOnMainSite = url.Contains("www.pixiv.net") && !url.Contains("accounts.pixiv.net");
            if (!isOnMainSite) return;

            _logger.LogInformation("已导航到 pixiv.net 主站，检测登录 Cookie");
            var cookies = await WebView.CoreWebView2.CookieManager.GetCookiesAsync("https://www.pixiv.net");
            var cookieStr = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
            if (cookieStr.Contains("p_"))
            {
                _logger.LogInformation("检测到 Pixiv 登录 Cookie (含 p_ 前缀)");
                await Capture(cookies);
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "检测登录状态异常"); }
    }

    private async Task Capture(IReadOnlyList<CoreWebView2Cookie> cookies)
    {
        if (_captured) return;
        _captured = true;

        var ordered = cookies.OrderByDescending(c => c.Name.StartsWith("p_") ? 1 : 0).ToList();
        var cookieStr = string.Join("; ", ordered.Select(c => $"{c.Name}={c.Value}"));
        await Dispatcher.InvokeAsync(() =>
        {
            _config.Cookie = cookieStr;
            _config.SaveConfig();
            StatusText.Text = "登录成功！Cookie 已保存";
            WaitRing.Visibility = Visibility.Collapsed;
        });

        _logger.LogInformation("Cookie 捕获成功，共 {Count} 个字段", ordered.Count);
        await Task.Delay(1000);
        await Dispatcher.InvokeAsync(() => { DialogResult = true; Close(); });
    }

    private async void OnManualCompleteClick(object s, RoutedEventArgs e)
    {
        if (_captured) return;
        try
        {
            var cookies = await WebView.CoreWebView2.CookieManager.GetCookiesAsync("https://www.pixiv.net");
            var cookieStr = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
            if (cookieStr.Contains("p_"))
            {
                await Capture(cookies);
            }
            else
            {
                StatusText.Text = "未检测到 Pixiv Cookie（需含 p_ 前缀）";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "检测失败，请重试";
            _logger.LogError(ex, "手动完成异常");
        }
    }

    private void OnReloadClick(object s, RoutedEventArgs e)
    {
        _captured = false;
        WebView.CoreWebView2.Navigate("https://accounts.pixiv.net/login?lang=zh");
        StatusText.Text = "等待登录...";
        WaitRing.Visibility = Visibility.Visible;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (!_captured) _logger.LogInformation("登录窗口已关闭，未捕获到 Cookie");
    }
}
