using PixivTools.ViewModels;

namespace PixivTools.Views;
public partial class RandomPicTab : System.Windows.Controls.UserControl
{
    public RandomPicTab(RandomPicViewModel vm) { InitializeComponent(); DataContext = vm; }
}
