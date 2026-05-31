using System.Windows;
using PixivTools.ViewModels;

namespace PixivTools.Views;

public partial class SettingsTab : System.Windows.Controls.UserControl
{
    private readonly SettingsViewModel _vm;
    public SettingsTab(SettingsViewModel vm) { InitializeComponent(); _vm = vm; DataContext = vm; }
    private void OnPicTypeChanged(object s, RoutedEventArgs e) { if (s is System.Windows.Controls.RadioButton rb && rb.IsChecked == true && rb.Tag is string t) _vm.PicType = t; }
    private void OnDlPicTypeChanged(object s, RoutedEventArgs e) { if (s is System.Windows.Controls.RadioButton rb && rb.IsChecked == true && rb.Tag is string t) _vm.DlPicType = t; }
}
