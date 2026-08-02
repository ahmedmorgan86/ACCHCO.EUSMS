using System.Windows;
using System.Windows.Controls;
using ACCHCO.EUSMS.App.ViewModels;

namespace ACCHCO.EUSMS.App.Views;

public partial class NetworkDevicesView : UserControl
{
    public NetworkDevicesView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<NetworkDevicesViewModel>();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is NetworkDevicesViewModel vm)
        {
            await vm.LoadDevicesCommand.ExecuteAsync(null);
            if (vm.Devices.Count == 0)
            {
                await vm.ScanNetworkCommand.ExecuteAsync(null);
            }
        }
    }
}
