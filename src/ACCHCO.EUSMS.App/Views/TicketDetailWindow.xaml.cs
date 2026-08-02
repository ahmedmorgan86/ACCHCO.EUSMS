using System.Windows;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.App.ViewModels;

namespace ACCHCO.EUSMS.App.Views;

public partial class TicketDetailWindow : Window
{
    public TicketDetailWindow(int? ticketId = null, NetworkDevice? device = null, Equipment? equipment = null)
    {
        InitializeComponent();

        var vm = App.ServiceProvider!.GetRequiredService<TicketDetailViewModel>();
        DataContext = vm;

        vm.RequestClose += () => Close();

        if (ticketId.HasValue)
        {
            vm.LoadTicketCommand.Execute(ticketId.Value);
        }
        else
        {
            vm.IsNewTicket = true;
            vm.IsEditMode = true;
            if (device != null)
                vm.InitializeForDevice(device);
            else if (equipment != null)
                vm.InitializeForEquipment(equipment);
        }
    }
}
