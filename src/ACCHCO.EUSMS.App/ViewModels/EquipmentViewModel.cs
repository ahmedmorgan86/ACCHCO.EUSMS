using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.Views;
using Serilog;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class EquipmentViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IEquipmentService _equipmentService;

    public EquipmentViewModel()
    {
        _equipmentService = App.ServiceProvider!.GetRequiredService<IEquipmentService>();

        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<Equipment> _equipmentList = new();
    [ObservableProperty] private Equipment? _selectedEquipment;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private bool _isNewEquipment;
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private EquipmentType? _filterType;
    [ObservableProperty] private EquipmentStatus? _filterStatus;
    [ObservableProperty] private string _filterLocation = string.Empty;

    [ObservableProperty] private Equipment _editEquipment = new();

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        var selectedId = SelectedEquipment?.Id;
        IsLoading = true;
        try
        {
            var list = await _equipmentService.GetAllAsync();
            EquipmentList = new ObservableCollection<Equipment>(list.OrderBy(e => e.Name, Comparer<string>.Create(NaturalCompare)));
            
            if (selectedId.HasValue)
                SelectedEquipment = EquipmentList.FirstOrDefault(e => e.Id == selectedId.Value);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "فشل تحميل المعدات");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task FilterAsync()
    {
        IsLoading = true;
        try
        {
            var filtered = await _equipmentService.GetFilteredAsync(
                FilterType, FilterLocation, null, FilterStatus, SearchText);
            EquipmentList = new ObservableCollection<Equipment>(filtered.OrderBy(e => e.Name, Comparer<string>.Create(NaturalCompare)));
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل التصفية: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static int NaturalCompare(string s1, string s2)
    {
        var parts1 = Regex.Split(s1, "([0-9]+)");
        var parts2 = Regex.Split(s2, "([0-9]+)");
        for (int i = 0; i < Math.Min(parts1.Length, parts2.Length); i++)
        {
            if (int.TryParse(parts1[i], out int n1) && int.TryParse(parts2[i], out int n2))
            {
                if (n1 != n2) return n1.CompareTo(n2);
            }
            else
            {
                int cmp = parts1[i].CompareTo(parts2[i]);
                if (cmp != 0) return cmp;
            }
        }
        return parts1.Length.CompareTo(parts2.Length);
    }

    [RelayCommand]
    private async Task AddNew()
    {
        var window = new EquipmentDetailWindow();
        window.ShowDialog();
        await LoadDataAsync();
    }

    [RelayCommand]
    private void CreateTicketForEquipment()
    {
        if (SelectedEquipment == null)
        {
            DialogHelper.ShowWarning("اختر معدة أولاً.");
            return;
        }
        WeakReferenceMessenger.Default.Send(new NavigateMessage("CreateTicket", SelectedEquipment));
    }

    [RelayCommand]
    private async Task DeleteEquipmentAsync()
    {
        if (SelectedEquipment == null) return;
        if (!DialogHelper.Confirm($"هل تريد حذف '{SelectedEquipment.Name}'؟", "تأكيد الحذف")) return;

        try
        {
            await _equipmentService.DeleteAsync(SelectedEquipment.Id, CurrentUser.Username);
            await LoadDataAsync();
            DialogHelper.ShowInfo("تم حذف المعدات.");
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل الحذف: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveEquipmentAsync()
    {
        IsLoading = true;
        try
        {
            if (EditEquipment.Id == 0)
                await _equipmentService.CreateAsync(EditEquipment, CurrentUser.Username);
            else
                await _equipmentService.UpdateAsync(EditEquipment, CurrentUser.Username);

            DialogHelper.ShowInfo("تم الحفظ بنجاح");
            IsEditMode = false;
            WeakReferenceMessenger.Default.Send(new EquipmentSavedMessage());
            await LoadDataAsync();
        }
        catch (Exception ex) 
        { 
            Log.Error(ex, "فشل حفظ المعدة");
            DialogHelper.ShowError($"فشل الحفظ: {ex.Message}"); 
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
    }
}
