using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.Views;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class IncidentManagementViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IIncidentService _incidentService;
    private readonly IConfigurationItemService _ciService;
    private readonly IAppUserService _userService;
    private readonly ISlaService _slaService;

    public IncidentManagementViewModel(
        IIncidentService incidentService,
        IConfigurationItemService ciService,
        IAppUserService userService,
        ISlaService slaService)
    {
        _incidentService = incidentService;
        _ciService = ciService;
        _userService = userService;
        _slaService = slaService;
        
        // قيم افتراضية لضمان عدم حدوث Binding Exception
        FilterSeverity = IncidentSeverity.Low;
        FilterStatus = IncidentStatus.New;
        FilterPriority = Priority.Medium;

        EditIncident = new Incident
        {
            Severity = IncidentSeverity.Low,
            Impact = IncidentImpact.SingleUser,
            Urgency = IncidentUrgency.Low,
            Priority = Priority.Low,
            Source = IncidentSource.Phone,
            EquipmentType = EquipmentType.PC,
            Shift = Shift.Red,
            WorkflowStatus = WorkflowStatus.New
        };

        _ = LoadDataAsync();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<Incident> _incidentList = new();
    [ObservableProperty] private Incident? _selectedIncident;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private Incident _editIncident = new();
    [ObservableProperty] private ObservableCollection<ConfigurationItem> _configurationItems = new();
    [ObservableProperty] private ObservableCollection<AppUser> _specialists = new();
    [ObservableProperty] private object? _selectedSlaStatus;

    [ObservableProperty] private IncidentSeverity? _filterSeverity;
    [ObservableProperty] private IncidentStatus? _filterStatus;
    [ObservableProperty] private Priority? _filterPriority;
    [ObservableProperty] private string _filterSearchText = string.Empty;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var selectedId = SelectedIncident?.Id;
            var items = await _incidentService.GetAllAsync();
            IncidentList = new ObservableCollection<Incident>(items);
            if (selectedId.HasValue)
                SelectedIncident = IncidentList.FirstOrDefault(i => i.Id == selectedId.Value);
            var cis = await _ciService.GetAllAsync();
            ConfigurationItems = new ObservableCollection<ConfigurationItem>(cis);
            var specialists = await _userService.GetActiveSpecialistsAsync();
            Specialists = new ObservableCollection<AppUser>(specialists);
        }
        catch (Exception ex) { Serilog.Log.Error(ex, "فشل تحميل الحوادث"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task FilterAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _incidentService.GetFilteredAsync(
                null, null, null, null, null, null,
                FilterPriority, FilterSeverity, FilterStatus, null, null);
            IncidentList = new ObservableCollection<Incident>(items);
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل التصفية: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddNew()
    {
        var window = new IncidentDetailWindow();
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedIncident == null) return;
        var window = new IncidentDetailWindow(SelectedIncident.Id);
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        try
        {
            if (EditIncident.Id == 0)
                await _incidentService.CreateAsync(EditIncident, CurrentUser.Username);
            else
                await _incidentService.UpdateAsync(EditIncident, CurrentUser.Username);

            WeakReferenceMessenger.Default.Send(new ShowSnackbarMessage("تم الحفظ بنجاح"));
            IsEditMode = false;
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الحفظ: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedIncident == null) return;
        if (!DialogHelper.Confirm("هل أنت متأكد من حذف هذا الحادث؟")) return;
        IsLoading = true;
        try
        {
            await _incidentService.DeleteAsync(SelectedIncident.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم الحذف بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الحذف: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ResolveAsync()
    {
        if (SelectedIncident == null) return;
        var notes = DialogHelper.ShowInput("حل الحادث", "أدخل ملاحظات الحل:");
        if (notes == null) return;
        IsLoading = true;
        try
        {
            await _incidentService.ResolveAsync(SelectedIncident.Id, notes, Environment.UserName);
            DialogHelper.ShowInfo("تم حل الحادث بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الحل: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        if (SelectedIncident == null) return;
        var notes = DialogHelper.ShowInput("إغلاق الحادث", "أدخل ملاحظات الإغلاق:");
        if (notes == null) return;
        IsLoading = true;
        try
        {
            await _incidentService.CloseAsync(SelectedIncident.Id, notes, "Resolved", true, Environment.UserName);
            DialogHelper.ShowInfo("تم إغلاق الحادث بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الإغلاق: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ReopenAsync()
    {
        if (SelectedIncident == null) return;
        if (!DialogHelper.Confirm("هل أنت متأكد من إعادة فتح الحادث؟")) return;
        IsLoading = true;
        try
        {
            await _incidentService.ReopenAsync(SelectedIncident.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم إعادة الفتح بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل إعادة الفتح: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
    }

    partial void OnSelectedIncidentChanged(Incident? value)
    {
        if (value != null)
        {
            _ = LoadSlaStatusAsync(value.Id);
        }
    }

    private async Task LoadSlaStatusAsync(int incidentId)
    {
        try
        {
            SelectedSlaStatus = await _slaService.GetSlaStatusAsync(incidentId);
        }
        catch { SelectedSlaStatus = null; }
    }
}
