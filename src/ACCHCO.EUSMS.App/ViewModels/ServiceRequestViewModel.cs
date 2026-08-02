using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.Views;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class ServiceRequestViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IServiceRequestService _requestService;

    public ServiceRequestViewModel(IServiceRequestService requestService)
    {
        _requestService = requestService;
        _ = LoadDataAsync();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<ServiceRequest> _requestList = new();
    [ObservableProperty] private ServiceRequest? _selectedRequest;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private ServiceRequest _editRequest = new();

    [ObservableProperty] private ServiceRequestType? _filterType;
    [ObservableProperty] private ServiceRequestStatus? _filterStatus;
    [ObservableProperty] private ApprovalStatus? _filterApprovalStatus;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var selectedId = SelectedRequest?.Id;
            var items = await _requestService.GetAllAsync();
            RequestList = new ObservableCollection<ServiceRequest>(items);
            if (selectedId.HasValue)
                SelectedRequest = RequestList.FirstOrDefault(r => r.Id == selectedId.Value);
        }
        catch (Exception ex) { Serilog.Log.Error(ex, "فشل تحميل طلبات الخدمة"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task FilterAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _requestService.GetFilteredAsync(FilterType, FilterStatus, FilterApprovalStatus, null, null, null);
            RequestList = new ObservableCollection<ServiceRequest>(items);
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل التصفية: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddNew()
    {
        var window = new ServiceRequestDetailWindow();
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedRequest == null) return;
        var window = new ServiceRequestDetailWindow(SelectedRequest.Id);
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        try
        {
            if (EditRequest.Id == 0)
                await _requestService.CreateAsync(EditRequest, Environment.UserName);
            else
                await _requestService.UpdateAsync(EditRequest, Environment.UserName);

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
        if (SelectedRequest == null) return;
        if (!DialogHelper.Confirm("هل أنت متأكد من حذف هذا الطلب؟")) return;
        IsLoading = true;
        try
        {
            await _requestService.DeleteAsync(SelectedRequest.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم الحذف بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الحذف: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (SelectedRequest == null) return;
        IsLoading = true;
        try
        {
            await _requestService.SubmitAsync(SelectedRequest.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم تقديم الطلب");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل التقديم: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        if (SelectedRequest == null) return;
        if (!DialogHelper.Confirm("هل تريد الموافقة على هذا الطلب؟")) return;
        IsLoading = true;
        try
        {
            await _requestService.ApproveAsync(SelectedRequest.Id, Environment.UserName, null, Environment.UserName);
            DialogHelper.ShowInfo("تمت الموافقة");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الموافقة: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RejectAsync()
    {
        if (SelectedRequest == null) return;
        var notes = DialogHelper.ShowInput("رفض الطلب", "أدخل سبب الرفض:");
        IsLoading = true;
        try
        {
            await _requestService.RejectAsync(SelectedRequest.Id, Environment.UserName, notes, Environment.UserName);
            DialogHelper.ShowInfo("تم رفض الطلب");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الرفض: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (SelectedRequest == null) return;
        IsLoading = true;
        try
        {
            await _requestService.StartAsync(SelectedRequest.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم بدء التنفيذ");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل البدء: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CompleteAsync()
    {
        if (SelectedRequest == null) return;
        var notes = DialogHelper.ShowInput("إتمام الطلب", "أدخل ملاحظات الإتمام:");
        IsLoading = true;
        try
        {
            await _requestService.CompleteAsync(SelectedRequest.Id, notes, Environment.UserName);
            DialogHelper.ShowInfo("تم إتمام الطلب");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الإتمام: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (SelectedRequest == null) return;
        if (!DialogHelper.Confirm("هل تريد إلغاء هذا الطلب؟")) return;
        IsLoading = true;
        try
        {
            await _requestService.CancelAsync(SelectedRequest.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم الإلغاء");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الإلغاء: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
    }
}
