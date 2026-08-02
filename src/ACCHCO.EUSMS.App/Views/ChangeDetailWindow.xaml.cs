using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.App.Views;

public partial class ChangeDetailWindow : Window
{
    public ChangeDetailWindow(int? changeId = null)
    {
        InitializeComponent();

        var vm = App.ServiceProvider!.GetRequiredService<ChangeManagementViewModel>();
        DataContext = vm;

        if (changeId.HasValue)
        {
            var service = App.ServiceProvider!.GetRequiredService<IChangeRequestService>();
            var change = service.GetByIdAsync(changeId.Value).GetAwaiter().GetResult();
            if (change != null)
            {
                vm.EditChange = change;
                vm.IsEditMode = true;
            }
        }
        else
        {
            vm.EditChange = new Data.Entities.ChangeRequest();
        }
    }
}
