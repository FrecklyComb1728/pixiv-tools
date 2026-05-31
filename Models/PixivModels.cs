using System.Text.Json.Serialization;

namespace PixivTools.Models;

public class PixivPageUrl
{
    [JsonPropertyName("thumb_mini")] public string? ThumbMini { get; set; }
    [JsonPropertyName("small")] public string? Small { get; set; }
    [JsonPropertyName("regular")] public string? Regular { get; set; }
    [JsonPropertyName("original")] public string? Original { get; set; }
}

public class PixivPage
{
    [JsonPropertyName("urls")] public PixivPageUrl? Urls { get; set; }
    [JsonPropertyName("width")] public int Width { get; set; }
    [JsonPropertyName("height")] public int Height { get; set; }
}

public class PixivPagesResponse
{
    [JsonPropertyName("error")] public bool Error { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("body")] public List<PixivPage>? Body { get; set; }
}

public class RandomPicData
{
    [JsonPropertyName("pid")] public long Pid { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("author")] public string? Author { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("upload_date")] public long UploadDate { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
}

public class RandomPicResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("data")] public List<RandomPicData>? Data { get; set; }
}

public class SearchArtworkItem
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("illustType")] public int IllustType { get; set; }
    [JsonPropertyName("xRestrict")] public int XRestrict { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("userId")] public string? UserId { get; set; }
    [JsonPropertyName("userName")] public string? UserName { get; set; }
    [JsonPropertyName("width")] public int Width { get; set; }
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("pageCount")] public int PageCount { get; set; }
    [JsonPropertyName("createDate")] public string? CreateDate { get; set; }
    [JsonPropertyName("aiType")] public int AiType { get; set; }
}

public class SearchIllustManga
{
    [JsonPropertyName("data")] public List<SearchArtworkItem>? Data { get; set; }
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("lastPage")] public int LastPage { get; set; }
}

public class SearchBody
{
    [JsonPropertyName("illustManga")] public SearchIllustManga? IllustManga { get; set; }
}

public class SearchResponse
{
    [JsonPropertyName("error")] public bool Error { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("body")] public SearchBody? Body { get; set; }
}

public class IllustDetailUrls
{
    [JsonPropertyName("mini")] public string? Mini { get; set; }
    [JsonPropertyName("thumb")] public string? Thumb { get; set; }
    [JsonPropertyName("small")] public string? Small { get; set; }
    [JsonPropertyName("regular")] public string? Regular { get; set; }
    [JsonPropertyName("original")] public string? Original { get; set; }
}

public class IllustTagItem
{
    [JsonPropertyName("tag")] public string? Tag { get; set; }
    [JsonPropertyName("translation")] public Dictionary<string, string>? Translation { get; set; }
}

public class IllustTags
{
    [JsonPropertyName("tags")] public List<IllustTagItem>? Tags { get; set; }
}

public class IllustDetailBody
{
    [JsonPropertyName("illustId")] public string? IllustId { get; set; }
    [JsonPropertyName("illustTitle")] public string? IllustTitle { get; set; }
    [JsonPropertyName("illustType")] public int IllustType { get; set; }
    [JsonPropertyName("uploadDate")] public string? UploadDate { get; set; }
    [JsonPropertyName("width")] public int Width { get; set; }
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("pageCount")] public int PageCount { get; set; }
    [JsonPropertyName("aiType")] public int AiType { get; set; }
    [JsonPropertyName("urls")] public IllustDetailUrls? Urls { get; set; }
    [JsonPropertyName("userId")] public string? UserId { get; set; }
    [JsonPropertyName("userName")] public string? UserName { get; set; }
    [JsonPropertyName("tags")] public IllustTags? Tags { get; set; }
}

public class IllustDetailResponse
{
    [JsonPropertyName("error")] public bool Error { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("body")] public IllustDetailBody? Body { get; set; }
}
