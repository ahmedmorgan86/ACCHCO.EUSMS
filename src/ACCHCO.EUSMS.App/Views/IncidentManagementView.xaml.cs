using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;

namespace ACCHCO.EUSMS.App.Views;

public partial class IncidentManagementView : UserControl
{
    public IncidentManagementView()
    {
        DataContext = App.ServiceProvider!.GetRequiredService<IncidentManagementViewModel>();
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is IncidentManagementViewModel vm)
            vm.LoadDataCommand.Execute(null);
    }
}
