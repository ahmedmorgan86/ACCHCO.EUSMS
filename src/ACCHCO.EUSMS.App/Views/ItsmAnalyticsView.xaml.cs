using System.Windows.Controls;

namespace ACCHCO.EUSMS.App.Views;

public partial class ItsmAnalyticsView : UserControl
{
    public ItsmAnalyticsView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<ViewModels.ItsmAnalyticsViewModel>();
    }
}
