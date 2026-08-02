using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.App.Views;

public partial class CmdbDetailWindow : Window
{
    public CmdbDetailWindow(int? ciId = null)
    {
        InitializeComponent();

        var vm = App.ServiceProvider!.GetRequiredService<CmdbViewModel>();
        DataContext = vm;

        if (ciId.HasValue)
        {
            var service = App.ServiceProvider!.GetRequiredService<IConfigurationItemService>();
            var ci = service.GetByIdAsync(ciId.Value).GetAwaiter().GetResult();
            if (ci != null)
            {
                vm.EditCi = ci;
                vm.IsEditMode = true;
            }
        }
        else
        {
            vm.EditCi = new Data.Entities.ConfigurationItem();
        }
    }
}
