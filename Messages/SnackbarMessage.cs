namespace PixivTools.Messages;

public class SnackbarMessage
{
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    public string Severity { get; init; } = "Info";
}
