using System.IO;

namespace PixivTools.Models;

public static class HistoryService
{
    private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "History_rec.txt");
    private const string Header = "[PIXIV_TOOLS.HISTORY]\nTime\t\t|\t PID\t| Multi | Page\n";

    public static void Append(string pid, bool multiPage, int page)
    {
        try
        {
            var exists = File.Exists(FilePath);
            if (!exists) File.WriteAllText(FilePath, Header);
            var content = File.ReadAllText(FilePath);
            if (!content.StartsWith("[PIXIV_TOOLS.HISTORY]"))
                File.WriteAllText(FilePath, Header + content);
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var pad = new string(' ', Math.Max(0, 11 - pid.Length));
            var line = $"{time} | {pid}{pad}| {multiPage}     | ";
            if (multiPage) line += page.ToString();
            line += "\n";
            File.AppendAllText(FilePath, line);
        }
        catch { }
    }
}
