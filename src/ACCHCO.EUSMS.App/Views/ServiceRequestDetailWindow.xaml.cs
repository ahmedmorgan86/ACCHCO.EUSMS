using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.App.Views;

public partial class ServiceRequestDetailWindow : Window
{
    public ServiceRequestDetailWindow(int? requestId = null)
    {
        InitializeComponent();

        var vm = App.ServiceProvider!.GetRequiredService<ServiceRequestViewModel>();
        DataContext = vm;

        if (requestId.HasValue)
        {
            var service = App.ServiceProvider!.GetRequiredService<IServiceRequestService>();
            var request = service.GetByIdAsync(requestId.Value).GetAwaiter().GetResult();
            if (request != null)
            {
                vm.EditRequest = request;
                vm.IsEditMode = true;
            }
        }
        else
        {
            vm.EditRequest = new Data.Entities.ServiceRequest();
        }
    }
}
