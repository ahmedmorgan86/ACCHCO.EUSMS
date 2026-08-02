using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;

namespace ACCHCO.EUSMS.App.Views;

public partial class PerformanceAnalyticsView : UserControl
{
    public PerformanceAnalyticsView()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider!.GetRequiredService<PerformanceAnalyticsViewModel>();
    }
}
