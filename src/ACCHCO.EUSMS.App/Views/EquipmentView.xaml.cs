using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;

namespace ACCHCO.EUSMS.App.Views;

public partial class EquipmentView : UserControl
{
    public EquipmentView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<EquipmentViewModel>();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is EquipmentViewModel vm)
            vm.LoadDataCommand.Execute(null);
    }
}
