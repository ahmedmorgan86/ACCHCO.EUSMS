using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class ItsmDashboardViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IIncidentService _incidentService;
    private readonly IChangeRequestService _changeService;
    private readonly IProblemService _problemService;
    private readonly IItsmAnalyticsService _analyticsService;

    public ItsmDashboardViewModel(
        IIncidentService incidentService,
        IChangeRequestService changeService,
        IProblemService problemService,
        IItsmAnalyticsService analyticsService)
    {
        _incidentService = incidentService;
        _changeService = changeService;
        _problemService = problemService;
        _analyticsService = analyticsService;
        _ = LoadDashboardDataAsync();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDashboardDataAsync();
    }

    [ObservableProperty] private int _totalOpenIncidents;
    [ObservableProperty] private int _totalResolvedIncidents;
    [ObservableProperty] private int _totalCriticalIncidents;
    [ObservableProperty] private int _totalEscalatedIncidents;
    [ObservableProperty] private int _totalOpenChanges;
    [ObservableProperty] private int _totalOpenProblems;
    [ObservableProperty] private double _mttr;
    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private ObservableCollection<object> _incidentTrendData = new();
    [ObservableProperty] private ObservableCollection<object> _topEquipmentData = new();
    [ObservableProperty] private ObservableCollection<object> _topDepartmentsData = new();

    [RelayCommand]
    private async Task LoadDashboardDataAsync()
    {
        IsLoading = true;
        try
        {
            var kpis = await _incidentService.GetDashboardKpisAsync();
            TotalOpenIncidents = kpis.TotalOpen;
            TotalResolvedIncidents = kpis.TotalResolved;
            TotalCriticalIncidents = kpis.TotalCritical;
            TotalEscalatedIncidents = kpis.TotalEscalated;
            Mttr = kpis.AverageResolutionTimeMinutes;

            var changes = await _changeService.GetAllAsync();
            TotalOpenChanges = changes.Count(c =>
                c.Status != ChangeStatus.Completed && c.Status != ChangeStatus.Cancelled);

            var problems = await _problemService.GetAllAsync();
            TotalOpenProblems = problems.Count(p =>
                p.Status != ProblemStatus.Resolved && p.Status != ProblemStatus.Closed);

            var trend = await _analyticsService.GetIncidentTrendAsync();
            IncidentTrendData = new ObservableCollection<object>(trend);

            var equipment = await _analyticsService.GetTopEquipmentFailuresAsync();
            TopEquipmentData = new ObservableCollection<object>(equipment);

            var departments = await _analyticsService.GetTopDepartmentsAsync();
            TopDepartmentsData = new ObservableCollection<object>(departments);
        }
        catch (Exception ex) { Serilog.Log.Error(ex, "فشل تحميل لوحة التحكم"); }
        finally { IsLoading = false; }
    }
}
