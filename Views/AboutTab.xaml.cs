using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace PixivTools.Views;

public partial class AboutTab : System.Windows.Controls.UserControl
{
    public AboutTab()
    {
        InitializeComponent();
        GithubInfo.Text = "作者：FrecklyComb1728\nC# / WPF / .NET 8\nCommunityToolkit.Mvvm / WebView2";
    }

    private void OnGitHubClick(object s, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/FrecklyComb1728") { UseShellExecute = true });
    }
}
