using System.Windows.Controls;

namespace ACCHCO.EUSMS.App.Views;

public partial class CmdbView : UserControl
{
    public CmdbView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<ViewModels.CmdbViewModel>();
    }
}
