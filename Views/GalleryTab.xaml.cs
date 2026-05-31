using System.Windows;
using System.Windows.Input;
using PixivTools.ViewModels;

namespace PixivTools.Views;
public partial class GalleryTab : System.Windows.Controls.UserControl
{
    private readonly GalleryViewModel _vm;
    public GalleryTab(GalleryViewModel vm) { InitializeComponent(); _vm = vm; DataContext = vm; Loaded += (_, _) => _vm.LoadCommand.Execute(null); }

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string pid)
            _vm.OpenPidCommand.Execute(pid);
    }

    private void OnPreviewBgClick(object sender, MouseButtonEventArgs e) => _vm.ClosePreviewCommand.Execute(null);

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string pid)
            if (MessageBox.Show($"确认删除 PID {pid} 及所有图片？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                _vm.DeleteCommand.Execute(pid);
    }
}
