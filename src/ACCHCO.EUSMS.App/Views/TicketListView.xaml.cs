using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;

namespace ACCHCO.EUSMS.App.Views;

public partial class TicketListView : UserControl
{
    public TicketListView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<TicketListViewModel>();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is TicketListViewModel vm)
        {
            vm.LoadDataCommand.Execute(null);
        }
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is TicketListViewModel vm && vm.SelectedTicket != null)
        {
            var detailWindow = new TicketDetailWindow(vm.SelectedTicket.Id);
            detailWindow.ShowDialog();
            vm.LoadDataCommand.Execute(null);
        }
    }

    private void OnContextMenuOpen(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is TicketListViewModel vm && vm.SelectedTicket != null)
        {
            var detailWindow = new TicketDetailWindow(vm.SelectedTicket.Id);
            detailWindow.ShowDialog();
            vm.LoadDataCommand.Execute(null);
        }
    }
}
