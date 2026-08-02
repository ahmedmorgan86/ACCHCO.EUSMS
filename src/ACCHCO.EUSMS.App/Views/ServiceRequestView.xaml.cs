using System.Windows.Controls;

namespace ACCHCO.EUSMS.App.Views;

public partial class ServiceRequestView : UserControl
{
    public ServiceRequestView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<ViewModels.ServiceRequestViewModel>();
    }
}
