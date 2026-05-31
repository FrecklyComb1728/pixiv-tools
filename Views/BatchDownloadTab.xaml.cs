using PixivTools.ViewModels;

namespace PixivTools.Views;
public partial class BatchDownloadTab : System.Windows.Controls.UserControl
{
    public BatchDownloadTab(BatchDownloadViewModel vm) { InitializeComponent(); DataContext = vm; }
}
