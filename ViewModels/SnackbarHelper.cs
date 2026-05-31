namespace PixivTools.ViewModels;

public static class SnackbarHelper
{
    public static Action<string, string, string>? ShowAction;

    public static void Show(string title, string message, string severity = "Info")
    {
        if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
            ShowAction?.Invoke(title, message, severity);
        else
            System.Windows.Application.Current?.Dispatcher.Invoke(() => ShowAction?.Invoke(title, message, severity));
    }
}
