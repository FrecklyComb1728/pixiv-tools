using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PixivTools.Models;

public class ArtworkRecord : INotifyPropertyChanged
{
    private object? _thumbnail;
    public object? Thumbnail { get => _thumbnail; set { _thumbnail = value; Notify(); } }

    public long Id { get; set; }
    public string Pid { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Tags { get; set; } = "";
    public string Url { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public int PageCount { get; set; }
    public int AiType { get; set; }
    public bool IsR18 { get; set; }
    public string FilePath { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime DownloadedAt { get; set; } = DateTime.Now;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
