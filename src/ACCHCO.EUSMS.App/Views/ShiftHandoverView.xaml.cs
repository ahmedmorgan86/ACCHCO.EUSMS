using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;

namespace ACCHCO.EUSMS.App.Views;

public partial class ShiftHandoverView : UserControl
{
    public ShiftHandoverView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<ShiftHandoverViewModel>();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShiftHandoverViewModel vm)
            vm.LoadDataCommand.Execute(null);
    }

    private async void OnNewHandoverClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShiftHandoverViewModel vm)
        {
            var window = new ShiftHandoverCreateWindow(vm)
            {
                Owner = Window.GetWindow(this)
            };
            var result = window.ShowDialog();
            if (result == true)
            {
                await vm.LoadDataAsync();
            }
        }
    }
}
