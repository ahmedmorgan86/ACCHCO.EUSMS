using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;

namespace ACCHCO.EUSMS.App.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<DashboardViewModel>();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            vm.LoadDataCommand.Execute(null);
        }
    }
}
