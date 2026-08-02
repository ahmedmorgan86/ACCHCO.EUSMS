using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.Views;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class UserManagementViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IAppUserService _userService;

    public UserManagementViewModel()
    {
        _userService = App.ServiceProvider!.GetRequiredService<IAppUserService>();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<AppUser> _users = new();
    [ObservableProperty] private AppUser? _selectedUser;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private bool _isNewUser;
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private AppUser _editUser = new();

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var selectedId = SelectedUser?.Id;
            var list = await _userService.GetAllAsync();
            Users = new ObservableCollection<AppUser>(list);
            if (selectedId.HasValue)
                SelectedUser = Users.FirstOrDefault(u => u.Id == selectedId.Value);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "فشل تحميل المستخدمين");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddNew()
    {
        var window = new UserDetailWindow();
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedUser == null) return;
        var window = new UserDetailWindow(SelectedUser.Id);
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        if (string.IsNullOrWhiteSpace(EditUser.Username) || string.IsNullOrWhiteSpace(EditUser.FullName))
        {
            DialogHelper.ShowWarning("اسم المستخدم والاسم الكامل مطلوبان.");
            return;
        }

        IsLoading = true;
        try
        {
            if (IsNewUser)
            {
                await _userService.CreateAsync(EditUser, CurrentUser.Username);
                DialogHelper.ShowInfo("تم إنشاء المستخدم.");
            }
            else
            {
                await _userService.UpdateAsync(EditUser, CurrentUser.Username);
                DialogHelper.ShowInfo("تم تحديث المستخدم.");
            }

            IsEditMode = false;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل الحفظ: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateUserAsync()
    {
        if (SelectedUser == null) return;
        if (!DialogHelper.Confirm($"هل تريد تعطيل المستخدم '{SelectedUser.Username}'؟", "تأكيد")) return;

        try
        {
            await _userService.DeactivateAsync(SelectedUser.Id, CurrentUser.Username);
            await LoadDataAsync();
            DialogHelper.ShowInfo("تم تعطيل المستخدم.");
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
    }
}
