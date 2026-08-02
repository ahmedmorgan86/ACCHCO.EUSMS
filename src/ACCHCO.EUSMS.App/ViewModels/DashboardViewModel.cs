using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class DashboardViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IDashboardService _dashboardService;
    private readonly ITicketService _ticketService;
    private readonly IShiftHandoverService _handoverService;

    public DashboardViewModel()
    {
        _dashboardService = App.ServiceProvider!.GetRequiredService<IDashboardService>();
        _ticketService = App.ServiceProvider!.GetRequiredService<ITicketService>();
        _handoverService = App.ServiceProvider!.GetRequiredService<IShiftHandoverService>();

        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private void CreateTicket()
    {
        WeakReferenceMessenger.Default.Send(new NavigateMessage("CreateTicket"));
    }

    [ObservableProperty] private int _todayTicketCount;
    [ObservableProperty] private int _openTicketCount;
    [ObservableProperty] private int _closedTicketCount;
    [ObservableProperty] private double _averageResolutionMinutes;
    [ObservableProperty] private int _totalEquipment;
    [ObservableProperty] private int _totalUsers;
    [ObservableProperty] private double _resolutionRate;
    [ObservableProperty] private int _criticalOpenTickets;
    [ObservableProperty] private int _highOpenTickets;
    [ObservableProperty] private ObservableCollection<Ticket> _recentTickets = new();
    [ObservableProperty] private ObservableCollection<ShiftHandover> _recentHandovers = new();
    [ObservableProperty] private ObservableCollection<KeyValuePair<string, int>> _ticketsByShift = new();
    [ObservableProperty] private ObservableCollection<KeyValuePair<string, int>> _ticketsBySpecialist = new();
    [ObservableProperty] private ObservableCollection<KeyValuePair<string, int>> _commonFaults = new();
    [ObservableProperty] private bool _isLoading;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var kpis = await _dashboardService.GetKpisAsync();
            TodayTicketCount = kpis.TotalTicketsToday;
            OpenTicketCount = kpis.OpenTickets;
            ClosedTicketCount = kpis.ClosedTickets;
            AverageResolutionMinutes = kpis.AverageResolutionMinutes;
            TotalEquipment = kpis.TotalEquipment;
            TotalUsers = kpis.TotalUsers;
            ResolutionRate = kpis.ResolutionRate;
            CriticalOpenTickets = kpis.CriticalOpenTickets;
            HighOpenTickets = kpis.HighOpenTickets;

            var recent = await _dashboardService.GetRecentTicketsAsync(10);
            RecentTickets = new ObservableCollection<Ticket>(recent);

            var handovers = await _handoverService.GetAllAsync();
            RecentHandovers = new ObservableCollection<ShiftHandover>(handovers.Take(5));

            var byShift = await _dashboardService.GetTicketsByShiftAsync();
            TicketsByShift = new ObservableCollection<KeyValuePair<string, int>>(
                byShift.Select(kvp => new KeyValuePair<string, int>(kvp.Key.ToString(), kvp.Value)));

            var bySpecialist = await _dashboardService.GetTicketsBySpecialistAsync();
            TicketsBySpecialist = new ObservableCollection<KeyValuePair<string, int>>(
                bySpecialist.Select(kvp => new KeyValuePair<string, int>(kvp.Key, kvp.Value)));

            var faults = await _dashboardService.GetMostCommonFaultsAsync();
            CommonFaults = new ObservableCollection<KeyValuePair<string, int>>(
                faults.Select(kvp => new KeyValuePair<string, int>(kvp.Key.ToString(), kvp.Value)));
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "فشل تحميل لوحة التحكم");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
