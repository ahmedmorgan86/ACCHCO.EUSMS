using System.Windows.Controls;

namespace ACCHCO.EUSMS.App.Views;

public partial class ProblemManagementView : UserControl
{
    public ProblemManagementView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<ViewModels.ProblemManagementViewModel>();
    }
}
