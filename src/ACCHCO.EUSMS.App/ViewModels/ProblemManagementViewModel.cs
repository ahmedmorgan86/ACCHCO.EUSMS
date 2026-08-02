using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.Views;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class ProblemManagementViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IProblemService _problemService;
    private readonly IIncidentService _incidentService;

    public ProblemManagementViewModel(IProblemService problemService, IIncidentService incidentService)
    {
        _problemService = problemService;
        _incidentService = incidentService;
        _ = LoadDataAsync();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<Problem> _problemList = new();
    [ObservableProperty] private Problem? _selectedProblem;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private Problem _editProblem = new();
    [ObservableProperty] private ObservableCollection<Incident> _linkedIncidents = new();
    [ObservableProperty] private ObservableCollection<Incident> _allIncidents = new();
    [ObservableProperty] private Incident? _selectedIncidentForLink;

    [ObservableProperty] private ProblemStatus? _filterStatus;
    [ObservableProperty] private string _filterSearchText = string.Empty;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var selectedId = SelectedProblem?.Id;
            var items = await _problemService.GetAllAsync();
            ProblemList = new ObservableCollection<Problem>(items);
            if (selectedId.HasValue)
                SelectedProblem = ProblemList.FirstOrDefault(p => p.Id == selectedId.Value);
            var incidents = await _incidentService.GetAllAsync();
            AllIncidents = new ObservableCollection<Incident>(incidents);
        }
        catch (Exception ex) { Serilog.Log.Error(ex, "فشل تحميل المشاكل"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task LoadLinkedIncidentsAsync()
    {
        if (SelectedProblem == null) return;
        try
        {
            var linked = await _problemService.GetLinkedIncidentsAsync(SelectedProblem.Id);
            LinkedIncidents = new ObservableCollection<Incident>(linked);
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل تحميل الحوادث المرتبطة: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task FilterAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _problemService.GetAllAsync();
            if (FilterStatus.HasValue)
                items = items.Where(p => p.Status == FilterStatus.Value);
            if (!string.IsNullOrWhiteSpace(FilterSearchText))
                items = items.Where(p => p.ProblemNumber.Contains(FilterSearchText) || p.Title.Contains(FilterSearchText));
            ProblemList = new ObservableCollection<Problem>(items);
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل التصفية: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddNew()
    {
        var window = new ProblemDetailWindow();
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedProblem == null) return;
        var window = new ProblemDetailWindow(SelectedProblem.Id);
        window.ShowDialog();
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        try
        {
            if (EditProblem.Id == 0)
                await _problemService.CreateAsync(EditProblem, Environment.UserName);
            else
                await _problemService.UpdateAsync(EditProblem, Environment.UserName);

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
        if (SelectedProblem == null) return;
        if (!DialogHelper.Confirm("هل أنت متأكد من حذف هذه المشكلة؟")) return;
        IsLoading = true;
        try
        {
            await _problemService.DeleteAsync(SelectedProblem.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم الحذف بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الحذف: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task LinkIncidentAsync()
    {
        if (SelectedProblem == null) return;
        if (SelectedIncidentForLink == null) { DialogHelper.ShowWarning("اختر حادثاً للربط"); return; }
        IsLoading = true;
        try
        {
            await _problemService.LinkIncidentAsync(SelectedProblem.Id, SelectedIncidentForLink.Id, null);
            DialogHelper.ShowInfo("تم ربط الحادث بنجاح");
            await LoadLinkedIncidentsAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الربط: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task UnlinkIncidentAsync(Incident incident)
    {
        if (SelectedProblem == null) return;
        IsLoading = true;
        try
        {
            await _problemService.UnlinkIncidentAsync(SelectedProblem.Id, incident.Id);
            DialogHelper.ShowInfo("تم فصل الحادث بنجاح");
            await LoadLinkedIncidentsAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الفصل: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task IdentifyRootCauseAsync()
    {
        if (SelectedProblem == null) return;
        var rootCause = DialogHelper.ShowInput("تحديد السبب الجذري", "أدخل السبب الجذري:");
        if (string.IsNullOrWhiteSpace(rootCause)) return;
        IsLoading = true;
        try
        {
            await _problemService.IdentifyRootCauseAsync(SelectedProblem.Id, rootCause, Environment.UserName);
            DialogHelper.ShowInfo("تم تحديد السبب الجذري بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل التحديد: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SetWorkaroundAsync()
    {
        if (SelectedProblem == null) return;
        var workaround = DialogHelper.ShowInput("الحل البديل", "أدخل الحل البديل:");
        if (string.IsNullOrWhiteSpace(workaround)) return;
        IsLoading = true;
        try
        {
            await _problemService.SetWorkaroundAsync(SelectedProblem.Id, workaround, Environment.UserName);
            DialogHelper.ShowInfo("تم تحديد الحل البديل بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل التحديد: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ResolveAsync()
    {
        if (SelectedProblem == null) return;
        var notes = DialogHelper.ShowInput("حل المشكلة", "أدخل ملاحظات الحل:");
        if (notes == null) return;
        IsLoading = true;
        try
        {
            await _problemService.ResolveAsync(SelectedProblem.Id, notes, Environment.UserName);
            DialogHelper.ShowInfo("تم حل المشكلة بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الحل: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        if (SelectedProblem == null) return;
        if (!DialogHelper.Confirm("هل تريد إغلاق هذه المشكلة؟")) return;
        IsLoading = true;
        try
        {
            await _problemService.CloseAsync(SelectedProblem.Id, Environment.UserName);
            DialogHelper.ShowInfo("تم إغلاق المشكلة بنجاح");
            await LoadDataAsync();
        }
        catch (Exception ex) { DialogHelper.ShowError($"فشل الإغلاق: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
    }
}
