using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.App.Views;

public partial class UserDetailWindow : Window
{
    public UserDetailWindow(int? userId = null)
    {
        InitializeComponent();

        var vm = App.ServiceProvider!.GetRequiredService<UserManagementViewModel>();
        DataContext = vm;

        if (userId.HasValue)
        {
            var service = App.ServiceProvider!.GetRequiredService<IAppUserService>();
            var user = service.GetByIdAsync(userId.Value).GetAwaiter().GetResult();
            if (user != null)
            {
                vm.EditUser = new AppUser
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Title = user.Title,
                    Section = user.Section,
                    Role = user.Role,
                    IsActive = user.IsActive
                };
                vm.IsEditMode = true;
            }
        }
        else
        {
            vm.EditUser = new AppUser();
            vm.IsNewUser = true;
        }
    }
}
