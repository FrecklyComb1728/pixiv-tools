using PixivTools.ViewModels;

namespace PixivTools.Views;
public partial class PidSearchTab : System.Windows.Controls.UserControl
{
    public PidSearchTab(PidSearchViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += (_, _) => PidInput.Focus();
    }
}
