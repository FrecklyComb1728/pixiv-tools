using System.IO;
using System.Windows.Media.Imaging;

namespace PixivTools.Services;

public static class ImageService
{
    public static string GetDefaultDestDir(string folderName, bool autoFolder)
        => autoFolder && !string.IsNullOrWhiteSpace(folderName) ? folderName : "Pixiv";

    public static string GetTempPath(string prefix)
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_pixivtools_");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{prefix}_{DateTime.Now:yyyyMMddHHmmss}.png");
    }

    public static void CleanTempFiles()
    {
        try
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_pixivtools_");
            if (!Directory.Exists(dir)) return;
            foreach (var f in Directory.GetFiles(dir))
            {
                var ext = Path.GetExtension(f).ToLower();
                if (ext is ".png" or ".jpg") try { File.Delete(f); } catch { }
            }
        }
        catch { }
    }

    public static BitmapImage? LoadImage(byte[] data)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(data);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch { return null; }
    }
}
