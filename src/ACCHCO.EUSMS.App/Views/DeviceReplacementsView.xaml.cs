using System.Windows.Controls;
using ACCHCO.EUSMS.App.ViewModels;

namespace ACCHCO.EUSMS.App.Views;

public partial class DeviceReplacementsView : UserControl
{
    public DeviceReplacementsView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<DeviceReplacementsViewModel>();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is DeviceReplacementsViewModel vm)
            await vm.LoadDataCommand.ExecuteAsync(null);
    }
}
