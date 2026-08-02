using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.Views;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class TicketListViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly ITicketService _ticketService;
    private readonly IAppUserService _userService;

    public TicketListViewModel()
    {
        _ticketService = App.ServiceProvider!.GetRequiredService<ITicketService>();
        _userService = App.ServiceProvider!.GetRequiredService<IAppUserService>();

        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<Ticket> _tickets = new();
    [ObservableProperty] private ObservableCollection<AppUser> _specialists = new();
    [ObservableProperty] private ObservableCollection<AppUser> _filterSpecialists = new();
    [ObservableProperty] private Ticket? _selectedTicket;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _searchText;

    [ObservableProperty] private DateTime? _filterDateFrom;
    [ObservableProperty] private DateTime? _filterDateTo;
    [ObservableProperty] private Shift? _filterShift;
    [ObservableProperty] private int? _filterSpecialistId;
    [ObservableProperty] private TicketStatus? _filterStatus;
    [ObservableProperty] private Priority? _filterPriority;
    [ObservableProperty] private EquipmentType? _filterEquipmentType;
    [ObservableProperty] private FaultType? _filterFaultType;

    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _openCount;
    [ObservableProperty] private int _closedCount;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        var selectedId = SelectedTicket?.Id;
        IsLoading = true;
        try
        {
            var specialists = await _userService.GetActiveSpecialistsAsync();
            Specialists = new ObservableCollection<AppUser>(specialists);

            var filterList = new List<AppUser> { new() { Id = 0, FullName = "الكل" } };
            filterList.AddRange(specialists.OrderBy(s => s.FullName));
            FilterSpecialists = new ObservableCollection<AppUser>(filterList);

            var allTickets = await _ticketService.GetAllAsync();
            Tickets = new ObservableCollection<Ticket>(allTickets);
            TotalCount = Tickets.Count;
            OpenCount = Tickets.Count(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress);
            ClosedCount = Tickets.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);
            
            if (selectedId.HasValue)
                SelectedTicket = Tickets.FirstOrDefault(t => t.Id == selectedId.Value);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "فشل تحميل التذاكر");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        IsLoading = true;
        try
        {
            var filtered = await _ticketService.GetFilteredAsync(
                FilterDateFrom, FilterDateTo, FilterShift,
                FilterSpecialistId is 0 ? null : FilterSpecialistId,
                null, FilterEquipmentType, FilterFaultType, FilterPriority, FilterStatus);
            Tickets = new ObservableCollection<Ticket>(filtered);
            TotalCount = Tickets.Count;
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل تصفية التذاكر: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        FilterDateFrom = null;
        FilterDateTo = null;
        FilterShift = null;
        FilterSpecialistId = null;
        FilterStatus = null;
        FilterPriority = null;
        FilterEquipmentType = null;
        FilterFaultType = null;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task CreateNewTicketAsync()
    {
        var window = new TicketDetailWindow();
        window.ShowDialog();
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteTicketAsync()
    {
        if (SelectedTicket == null) return;
        if (!DialogHelper.Confirm($"هل تريد حذف التذكرة {SelectedTicket.TicketNumber}؟", "تأكيد الحذف")) return;

        try
        {
            await _ticketService.DeleteAsync(SelectedTicket.Id, CurrentUser.Username);
            await LoadDataAsync();
            DialogHelper.ShowInfo("تم حذف التذكرة بنجاح.");
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل حذف التذكرة: {ex.Message}");
        }
    }

    partial void OnSearchTextChanging(string? oldValue, string? newValue)
    {
        _ = SearchAsync(newValue);
    }

    private async Task SearchAsync(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            await LoadDataAsync();
            return;
        }

        var all = await _ticketService.GetAllAsync();
        var filtered = all.Where(t =>
            t.TicketNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            t.RequesterName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            (t.ComputerName != null && t.ComputerName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
            (t.EquipmentName != null && t.EquipmentName.Contains(search, StringComparison.OrdinalIgnoreCase)));
        Tickets = new ObservableCollection<Ticket>(filtered);
    }
}
