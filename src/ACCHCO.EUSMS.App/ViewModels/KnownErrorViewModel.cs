using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.Views;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class KnownErrorViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IKnownErrorService _knownErrorService;

    public KnownErrorViewModel(IKnownErrorService knownErrorService)
    {
        _knownErrorService = knownErrorService;
        _ = LoadDataAsync();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<KnownError> _knownErrorList = new();
    [ObservableProperty] private KnownError? _selectedKnownError;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private KnownError _editKnownError = new();

    [ObservableProperty] private KnownErrorStatus? _filterStatus;
    [ObservableProperty] private string _filterSearchText = string.Empty;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var selectedId = SelectedKnownError?.Id;
            var items = await _knownErrorService.GetAllAsync();
            KnownErrorList = new ObservableCollection<KnownError>(items);
            if (selectedId.HasValue)
                SelectedKnownError = KnownErrorList.FirstOrDefault(k => k.Id == selectedId.Value);
        }
        catch (Exception ex) { Serilog.Log.Error(ex, "فشل تحميل الأخطاء المعروفة"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task FilterAsync()
    {
        IsLoading = true;
        try
        {
            IEnumerable<KnownError> items;
            if (FilterStatus.HasValue)
                items = await _knownErrorService.GetByStatusAsync(FilterStatus.Value);
            else if (!string.IsNullOrWhiteSpace(FilterSearchText))
                items = await _knownErrorService.SearchAsync(FilterSearchText);
            else
                items = await _knownErrorService.GetAllAsync();

            KnownErrorList = new ObservableCollection<KnownError>(items);
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل التصفية: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddNew()
    {
        var window = new KnownErrorDetailWindow();
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedKnownError == null) return;
        var window = new KnownErrorDetailWindow(SelectedKnownError.Id);
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        try
        {
            if (EditKnownError.Id == 0)
                await _knownErrorService.CreateAsync(EditKnownError, Environment.UserName);
            else
                await _knownErrorService.UpdateAsync(EditKnownError, Environment.UserName);

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
        if (SelectedKnownError == null) return;
        if (!DialogHelper.Confirm("هل أنت متأكد من حذف هذا الخطأ المعروف؟")) return;
        IsLoading = true;
        try
        {
            await _knownErrorService.DeleteAsync(SelectedKnownError.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم الحذف بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الحذف: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
    }
}
