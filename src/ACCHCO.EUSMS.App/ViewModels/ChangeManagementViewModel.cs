using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.Views;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class ChangeManagementViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IChangeRequestService _changeService;

    public ChangeManagementViewModel(IChangeRequestService changeService)
    {
        _changeService = changeService;
        _ = LoadDataAsync();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<ChangeRequest> _changeList = new();
    [ObservableProperty] private ChangeRequest? _selectedChange;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private ChangeRequest _editChange = new();

    [ObservableProperty] private ChangeType? _filterChangeType;
    [ObservableProperty] private ChangeStatus? _filterStatus;
    [ObservableProperty] private ChangeRisk? _filterRisk;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var selectedId = SelectedChange?.Id;
            var items = await _changeService.GetAllAsync();
            ChangeList = new ObservableCollection<ChangeRequest>(items);
            if (selectedId.HasValue)
                SelectedChange = ChangeList.FirstOrDefault(c => c.Id == selectedId.Value);
        }
        catch (Exception ex) { Serilog.Log.Error(ex, "فشل تحميل طلبات التغيير"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task FilterAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _changeService.GetFilteredAsync(FilterChangeType, FilterStatus, FilterRisk, null, null, null);
            ChangeList = new ObservableCollection<ChangeRequest>(items);
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل التصفية: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddNew()
    {
        var window = new ChangeDetailWindow();
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedChange == null) return;
        var window = new ChangeDetailWindow(SelectedChange.Id);
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        try
        {
            if (EditChange.Id == 0)
                await _changeService.CreateAsync(EditChange, Environment.UserName);
            else
                await _changeService.UpdateAsync(EditChange, Environment.UserName);

            DialogHelper.ShowInfo("تم الحفظ بنجاح");
            IsEditMode = false;
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الحفظ: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedChange == null) return;
        if (!DialogHelper.Confirm("هل أنت متأكد من حذف هذا الطلب؟")) return;
        IsLoading = true;
        try
        {
            await _changeService.DeleteAsync(SelectedChange.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم الحذف بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الحذف: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (SelectedChange == null) return;
        IsLoading = true;
        try
        {
            await _changeService.SubmitAsync(SelectedChange.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم تقديم الطلب للموافقة");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل التقديم: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        if (SelectedChange == null) return;
        if (!DialogHelper.Confirm("هل تريد الموافقة على هذا الطلب؟")) return;
        IsLoading = true;
        try
        {
            await _changeService.ApproveAsync(SelectedChange.Id, Environment.UserName, null, Environment.UserName);
            DialogHelper.ShowInfo("تمت الموافقة بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الموافقة: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RejectAsync()
    {
        if (SelectedChange == null) return;
        var notes = DialogHelper.ShowInput("رفض الطلب", "أدخل سبب الرفض:");
        IsLoading = true;
        try
        {
            await _changeService.RejectAsync(SelectedChange.Id, Environment.UserName, notes, Environment.UserName);
            DialogHelper.ShowInfo("تم رفض الطلب");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الرفض: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task StartImplementationAsync()
    {
        if (SelectedChange == null) return;
        IsLoading = true;
        try
        {
            await _changeService.StartImplementationAsync(SelectedChange.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم بدء التنفيذ");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل بدء التنفيذ: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CompleteAsync()
    {
        if (SelectedChange == null) return;
        var notes = DialogHelper.ShowInput("إتمام التنفيذ", "أدخل ملاحظات الإتمام:");
        IsLoading = true;
        try
        {
            await _changeService.CompleteAsync(SelectedChange.Id, notes, null, Environment.UserName);
            DialogHelper.ShowInfo("تم إتمام التنفيذ بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الإتمام: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RollbackAsync()
    {
        if (SelectedChange == null) return;
        if (!DialogHelper.Confirm("هل تريد التراجع عن هذا التغيير؟")) return;
        IsLoading = true;
        try
        {
            await _changeService.RollbackAsync(SelectedChange.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم التراجع بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل التراجع: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
    }
}
