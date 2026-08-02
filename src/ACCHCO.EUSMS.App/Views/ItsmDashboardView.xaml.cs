using System.Windows.Controls;

namespace ACCHCO.EUSMS.App.Views;

public partial class ItsmDashboardView : UserControl
{
    public ItsmDashboardView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<ViewModels.ItsmDashboardViewModel>();
    }
}
