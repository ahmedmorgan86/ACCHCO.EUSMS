using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.App.Views;

public partial class ProblemDetailWindow : Window
{
    public ProblemDetailWindow(int? problemId = null)
    {
        InitializeComponent();

        var vm = App.ServiceProvider!.GetRequiredService<ProblemManagementViewModel>();
        DataContext = vm;

        if (problemId.HasValue)
        {
            var service = App.ServiceProvider!.GetRequiredService<IProblemService>();
            var problem = service.GetByIdAsync(problemId.Value).GetAwaiter().GetResult();
            if (problem != null)
            {
                vm.EditProblem = problem;
                vm.IsEditMode = true;
            }
        }
        else
        {
            vm.EditProblem = new Data.Entities.Problem();
        }
    }
}
