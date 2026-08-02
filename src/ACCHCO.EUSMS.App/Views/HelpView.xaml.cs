using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;

namespace ACCHCO.EUSMS.App.Views;

public partial class HelpView : UserControl
{
    public HelpView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<HelpViewModel>();
    }
}
