using System.Windows.Controls;

namespace ACCHCO.EUSMS.App.Views;

public partial class ChangeManagementView : UserControl
{
    public ChangeManagementView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<ViewModels.ChangeManagementViewModel>();
    }
}
