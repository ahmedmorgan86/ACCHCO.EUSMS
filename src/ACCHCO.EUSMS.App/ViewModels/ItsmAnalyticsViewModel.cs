using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class ItsmAnalyticsViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IItsmAnalyticsService _analyticsService;

    public ItsmAnalyticsViewModel(IItsmAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
        _ = LoadAnalyticsAsync();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadAnalyticsAsync();
    }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private double _mttr;
    [ObservableProperty] private double _mttb;
    [ObservableProperty] private double _fcr;

    [ObservableProperty] private ObservableCollection<object> _incidentByPriority = new();
    [ObservableProperty] private ObservableCollection<object> _incidentBySeverity = new();
    [ObservableProperty] private ObservableCollection<object> _incidentByStatus = new();
    [ObservableProperty] private ObservableCollection<object> _problemByStatus = new();
    [ObservableProperty] private ObservableCollection<object> _changeByStatus = new();
    [ObservableProperty] private ObservableCollection<object> _topSpecialists = new();
    [ObservableProperty] private ObservableCollection<object> _incidentTrend = new();

    [RelayCommand]
    private async Task LoadAnalyticsAsync()
    {
        IsLoading = true;
        try
        {
            Mttr = await _analyticsService.GetMttRAsync();
            Mttb = await _analyticsService.GetMttBAsync();
            Fcr = await _analyticsService.GetFcrAsync();

            var byPriority = await _analyticsService.GetIncidentsByPriorityAsync();
            IncidentByPriority = new ObservableCollection<object>(byPriority);

            var bySeverity = await _analyticsService.GetIncidentsBySeverityAsync();
            IncidentBySeverity = new ObservableCollection<object>(bySeverity);

            var byStatus = await _analyticsService.GetIncidentsByStatusAsync();
            IncidentByStatus = new ObservableCollection<object>(byStatus);

            var problemsByStatus = await _analyticsService.GetProblemsByStatusAsync();
            ProblemByStatus = new ObservableCollection<object>(problemsByStatus);

            var changesByStatus = await _analyticsService.GetChangesByStatusAsync();
            ChangeByStatus = new ObservableCollection<object>(changesByStatus);

            var specialists = await _analyticsService.GetTopSpecialistsAsync();
            TopSpecialists = new ObservableCollection<object>(specialists);

            var trend = await _analyticsService.GetIncidentTrendAsync();
            IncidentTrend = new ObservableCollection<object>(trend);
        }
        catch (Exception ex) { Serilog.Log.Error(ex, "فشل تحميل التحليلات"); }
        finally { IsLoading = false; }
    }
}
