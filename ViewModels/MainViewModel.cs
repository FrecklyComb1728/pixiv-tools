using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace PixivTools.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private bool _isSnackbarOpen;

    [ObservableProperty]
    private string _snackbarTitle = "";

    [ObservableProperty]
    private string _snackbarMessage = "";

    [ObservableProperty]
    private string _snackbarSeverity = "Info";

    public MainViewModel(ILogger<MainViewModel> logger)
    {
        _logger = logger;
        SnackbarHelper.ShowAction = (title, msg, severity) =>
        {
            SnackbarTitle = title;
            SnackbarMessage = msg;
            SnackbarSeverity = severity;
            IsSnackbarOpen = true;
        };
        _logger.LogInformation("PixivTools 4.0 启动");
    }

    [RelayCommand]
    private void CloseSnackbar() => IsSnackbarOpen = false;
}
