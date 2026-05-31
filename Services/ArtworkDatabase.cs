using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using PixivTools.Models;

namespace PixivTools.Services;

public class ArtworkDatabase : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ILogger<ArtworkDatabase> _logger;

    public ArtworkDatabase(ILogger<ArtworkDatabase> logger)
    {
        _logger = logger;
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_pixivtools_", "artworks.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        InitSchema();
        _logger.LogInformation("数据库已打开: {Path}", dbPath);
    }

    private void InitSchema()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS artworks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                pid TEXT NOT NULL,
                title TEXT,
                author TEXT,
                tags TEXT,
                url TEXT,
                width INTEGER,
                height INTEGER,
                page_count INTEGER DEFAULT 1,
                ai_type INTEGER DEFAULT 0,
                is_r18 INTEGER DEFAULT 0,
                file_path TEXT,
                file_size INTEGER DEFAULT 0,
                downloaded_at TEXT DEFAULT (datetime('now','localtime'))
            );
            CREATE INDEX IF NOT EXISTS idx_pid ON artworks(pid);
            CREATE INDEX IF NOT EXISTS idx_author ON artworks(author);
            CREATE INDEX IF NOT EXISTS idx_downloaded_at ON artworks(downloaded_at);
            """;
        cmd.ExecuteNonQuery();
    }

    public void Insert(ArtworkRecord r)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO artworks(pid, title, author, tags, url, width, height, page_count, ai_type, is_r18, file_path, file_size, downloaded_at)
            VALUES(@pid, @title, @author, @tags, @url, @width, @height, @page, @ai, @r18, @fp, @fs, datetime('now','localtime'))
            """;
        cmd.Parameters.AddWithValue("@pid", r.Pid);
        cmd.Parameters.AddWithValue("@title", r.Title);
        cmd.Parameters.AddWithValue("@author", r.Author);
        cmd.Parameters.AddWithValue("@tags", r.Tags);
        cmd.Parameters.AddWithValue("@url", r.Url);
        cmd.Parameters.AddWithValue("@width", r.Width);
        cmd.Parameters.AddWithValue("@height", r.Height);
        cmd.Parameters.AddWithValue("@page", r.PageCount);
        cmd.Parameters.AddWithValue("@ai", r.AiType);
        cmd.Parameters.AddWithValue("@r18", r.IsR18 ? 1 : 0);
        cmd.Parameters.AddWithValue("@fp", r.FilePath);
        cmd.Parameters.AddWithValue("@fs", r.FileSize);
        cmd.ExecuteNonQuery();
        _logger.LogDebug("已入库: PID={Pid}", r.Pid);
    }

    public List<ArtworkRecord> Query(string? search, string? tags, string? author, string sort, int limit, int offset)
    {
        var where = new List<string> { "1=1" };
        var parameters = new List<SqliteParameter>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search;
            if (s.StartsWith("t:")) { where.Add("title LIKE @ks"); parameters.Add(new SqliteParameter("@ks", $"%{s[2..]}%")); }
            else if (s.StartsWith("a:")) { where.Add("author LIKE @ks"); parameters.Add(new SqliteParameter("@ks", $"%{s[2..]}%")); }
            else if (s.StartsWith("g:")) { where.Add("tags LIKE @ks"); parameters.Add(new SqliteParameter("@ks", $"%{s[2..]}%")); }
            else if (s.All(char.IsDigit)) { where.Add("pid = @ks"); parameters.Add(new SqliteParameter("@ks", s)); }
            else { where.Add("(title LIKE @ks OR pid LIKE @ks OR tags LIKE @ks OR author LIKE @ks)"); parameters.Add(new SqliteParameter("@ks", $"%{s}%")); }
        }
        if (!string.IsNullOrWhiteSpace(tags))
        {
            foreach (var t in tags.Split(',', StringSplitOptions.TrimEntries))
            {
                where.Add($"tags LIKE @t{parameters.Count}");
                parameters.Add(new SqliteParameter($"@t{parameters.Count}", $"%{t.Trim()}%"));
            }
        }
        if (!string.IsNullOrWhiteSpace(author))
        {
            where.Add("author LIKE @a");
            parameters.Add(new SqliteParameter("@a", $"%{author}%"));
        }

        var order = sort switch
        {
            "date-asc" => "downloaded_at ASC",
            "pages-desc" => "page_count DESC, downloaded_at DESC",
            "tags-desc" => "(SELECT COUNT(*) FROM artworks a2 WHERE a2.tags != '' AND a2.id = artworks.id) DESC, downloaded_at DESC",
            "author-desc" => "author DESC, downloaded_at DESC",
            _ => "downloaded_at DESC"
        };

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM artworks WHERE {string.Join(" AND ", where)} ORDER BY {order} LIMIT @limit OFFSET @offset";
        cmd.Parameters.AddRange(parameters);
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);

        var results = new List<ArtworkRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new ArtworkRecord
            {
                Id = reader.GetInt64(0),
                Pid = reader.GetString(1),
                Title = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Author = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Tags = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Url = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Width = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                Height = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                PageCount = reader.IsDBNull(8) ? 1 : reader.GetInt32(8),
                AiType = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                IsR18 = !reader.IsDBNull(10) && reader.GetInt32(10) == 1,
                FilePath = reader.IsDBNull(11) ? "" : reader.GetString(11),
                FileSize = reader.IsDBNull(12) ? 0 : reader.GetInt64(12),
                DownloadedAt = reader.IsDBNull(13) ? DateTime.Now : reader.GetDateTime(13)
            });
        }
        return results;
    }

    public int Count(string? search, string? tags, string? author)
    {
        var where = new List<string> { "1=1" };
        if (!string.IsNullOrWhiteSpace(search))
            where.Add("(title LIKE @s OR pid LIKE @s)");
        if (!string.IsNullOrWhiteSpace(author))
            where.Add("author LIKE @a");

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM artworks WHERE {string.Join(" AND ", where)}";
        if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
        if (!string.IsNullOrWhiteSpace(author)) cmd.Parameters.AddWithValue("@a", $"%{author}%");

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<string> GetAllTags()
    {
        var tags = new HashSet<string>();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT tags FROM artworks";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var t = reader.GetString(0);
            foreach (var tag in t.Split(',', StringSplitOptions.TrimEntries))
                if (!string.IsNullOrWhiteSpace(tag)) tags.Add(tag);
        }
        return tags.OrderBy(t => t).ToList();
    }

    public void Delete(string pid)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM artworks WHERE pid = @pid";
        cmd.Parameters.AddWithValue("@pid", pid);
        cmd.ExecuteNonQuery();
        _logger.LogInformation("已删除: PID={Pid}", pid);
    }

    public void Dispose() => _conn?.Dispose();
}
