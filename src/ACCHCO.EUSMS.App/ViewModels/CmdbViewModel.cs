using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.Views;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class CmdbViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IConfigurationItemService _ciService;

    public CmdbViewModel(IConfigurationItemService ciService)
    {
        _ciService = ciService;
        _ = LoadDataAsync();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<ConfigurationItem> _ciList = new();
    [ObservableProperty] private ConfigurationItem? _selectedCi;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private ConfigurationItem _editCi = new();

    [ObservableProperty] private EquipmentType? _filterType;
    [ObservableProperty] private string _filterSearchText = string.Empty;
    [ObservableProperty] private string _filterDepartment = string.Empty;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var selectedId = SelectedCi?.Id;
            var items = await _ciService.GetAllAsync();
            CiList = new ObservableCollection<ConfigurationItem>(items);
            if (selectedId.HasValue)
                SelectedCi = CiList.FirstOrDefault(c => c.Id == selectedId.Value);
        }
        catch (Exception ex) { Serilog.Log.Error(ex, "فشل تحميل عناصر التكوين"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task FilterAsync()
    {
        IsLoading = true;
        try
        {
            IEnumerable<ConfigurationItem> items;
            if (FilterType.HasValue)
                items = await _ciService.GetByTypeAsync(FilterType.Value);
            else if (!string.IsNullOrWhiteSpace(FilterDepartment))
                items = await _ciService.GetByDepartmentAsync(FilterDepartment);
            else if (!string.IsNullOrWhiteSpace(FilterSearchText))
                items = await _ciService.SearchAsync(FilterSearchText);
            else
                items = await _ciService.GetAllAsync();

            CiList = new ObservableCollection<ConfigurationItem>(items);
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل التصفية: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddNew()
    {
        var window = new CmdbDetailWindow();
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedCi == null) return;
        var window = new CmdbDetailWindow(SelectedCi.Id);
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        try
        {
            if (EditCi.Id == 0)
                await _ciService.CreateAsync(EditCi, Environment.UserName);
            else
                await _ciService.UpdateAsync(EditCi, Environment.UserName);

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
        if (SelectedCi == null) return;
        if (!DialogHelper.Confirm("هل أنت متأكد من حذف عنصر التكوين؟")) return;
        IsLoading = true;
        try
        {
            await _ciService.DeleteAsync(SelectedCi.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم الحذف بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الحذف: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(FilterSearchText)) { await LoadDataAsync(); return; }
        IsLoading = true;
        try
        {
            var items = await _ciService.SearchAsync(FilterSearchText);
            CiList = new ObservableCollection<ConfigurationItem>(items);
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل البحث: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
    }
}
