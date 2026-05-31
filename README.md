# pixiv-tools

Pixiv 桌面工具箱 — 无需代理即可查看和下载 Pixiv 图片。

## 功能

- **PID 查图** — 输入作品 ID 直接查看/下载 Pixiv 图片，支持多页图集翻页浏览
- **随机图** — 基于 Pixiv 搜索 API，关键词/标签随机获取作品
- **批量下载** — PID 队列管理，带进度条，并行缓存加速
- **浏览器登录** — 内嵌 WebView2 浏览器登录，自动捕获 Cookie
- **代理支持** — HTTP / SOCKS4 / SOCKS5 代理配置

## 技术栈

- .NET 8 + WPF
- CommunityToolkit.Mvvm (MVVM 架构)
- Microsoft WebView2 (浏览器登录)
- System.Text.Json (API 解析)

## 下载

从 [Releases](https://github.com/FrecklyComb1728/pixiv-tools/releases) 下载最新版本 `pixiv-tools-win-x64.zip`，解压后运行 `pixiv-tools.exe`。

.NET 8 运行时已内嵌，无需额外安装。

## 构建

```bash
dotnet build pixiv-tools.csproj --configuration Release
```

发布自包含单文件：

```bash
dotnet publish pixiv-tools.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## 许可

MIT License
