using System.Windows.Controls;

namespace ACCHCO.EUSMS.App.Views;

public partial class KnownErrorView : UserControl
{
    public KnownErrorView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<ViewModels.KnownErrorViewModel>();
    }
}
