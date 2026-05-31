using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PixivTools.Models;

public class AppConfig
{
    public bool ProxyEnabled { get; set; }
    public string ProxyType { get; set; } = "http";
    public string ProxyAddr { get; set; } = "";
    public bool Mirror { get; set; } = true;
    public bool Remember { get; set; } = true;
    public string PicType { get; set; } = "png";
    public string DlPicType { get; set; } = "png";
    public bool AutoFolder { get; set; } = true;
    public string FolderName { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Pixiv");
    public bool Nsfw { get; set; }
    public bool MultiPage { get; set; }
    public int Page { get; set; } = 1;
    public string Cookie { get; set; } = "";
    public string Theme { get; set; } = "System";
    public int PreDownloadCount { get; set; } = 3;

    [JsonIgnore]
    public static string ConfigDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_pixivtools_");
    [JsonIgnore]
    public static string ConfigFile => "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static AppConfig Load()
    {
        var path = Path.Combine(ConfigDir, ConfigFile);
        if (!File.Exists(path))
            return new AppConfig();
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"配置加载失败: {ex.Message}");
            return new AppConfig();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var path = Path.Combine(ConfigDir, ConfigFile);
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch { }
    }
}
