using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _currentView = "Dashboard";
    [ObservableProperty] private string _currentUser = Environment.UserName;
    [ObservableProperty] private string _appTitle = "ACCHCO EUSMS";
}
