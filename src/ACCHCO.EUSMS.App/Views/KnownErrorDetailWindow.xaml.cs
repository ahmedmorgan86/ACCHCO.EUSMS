using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.App.Views;

public partial class KnownErrorDetailWindow : Window
{
    public KnownErrorDetailWindow(int? knownErrorId = null)
    {
        InitializeComponent();

        var vm = App.ServiceProvider!.GetRequiredService<KnownErrorViewModel>();
        DataContext = vm;

        if (knownErrorId.HasValue)
        {
            var service = App.ServiceProvider!.GetRequiredService<IKnownErrorService>();
            var knownError = service.GetByIdAsync(knownErrorId.Value).GetAwaiter().GetResult();
            if (knownError != null)
            {
                vm.EditKnownError = knownError;
                vm.IsEditMode = true;
            }
        }
        else
        {
            vm.EditKnownError = new Data.Entities.KnownError();
        }
    }
}
