using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class ShiftHandoverViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IShiftHandoverService _handoverService;
    private readonly ITicketService _ticketService;
    private readonly IAppUserService _userService;

    public ShiftHandoverViewModel()
    {
        _handoverService = App.ServiceProvider!.GetRequiredService<IShiftHandoverService>();
        _ticketService = App.ServiceProvider!.GetRequiredService<ITicketService>();
        _userService = App.ServiceProvider!.GetRequiredService<IAppUserService>();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<ShiftHandover> _handovers = new();
    [ObservableProperty] private ShiftHandover? _selectedHandover;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isCreating;
    [ObservableProperty] private ObservableCollection<AppUser> _specialists = new();
    [ObservableProperty] private ObservableCollection<Ticket> _pendingTickets = new();
    [ObservableProperty] private ObservableCollection<Ticket> _selectedHandoverTickets = new();

    [ObservableProperty] private ShiftHandover _newHandover = new();
    [ObservableProperty] private ObservableCollection<int> _selectedTicketIds = new();

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var selectedId = SelectedHandover?.Id;
            var list = await _handoverService.GetAllAsync();
            Handovers = new ObservableCollection<ShiftHandover>(list);
            if (selectedId.HasValue)
                SelectedHandover = Handovers.FirstOrDefault(h => h.Id == selectedId.Value);

            var specialists = await _userService.GetActiveSpecialistsAsync();
            Specialists = new ObservableCollection<AppUser>(specialists);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "فشل تحميل التسليم");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadPendingTicketsAsync()
    {
        try
        {
            var tickets = await _handoverService.GetPendingTicketsForHandoverAsync(
                NewHandover.FromShift, NewHandover.HandoverDate);
            PendingTickets = new ObservableCollection<Ticket>(tickets);
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void StartCreate()
    {
        NewHandover = new ShiftHandover
        {
            HandoverDate = DateTime.Today,
            FromShift = Shift.Red,
            ToShift = Shift.Yellow
        };
        SelectedTicketIds.Clear();
        PendingTickets.Clear();
        IsCreating = true;
    }

    [RelayCommand]
    private async Task CreateHandoverAsync()
    {
        if (string.IsNullOrWhiteSpace(NewHandover.Summary))
        {
            DialogHelper.ShowWarning("الملخص مطلوب.");
            return;
        }

        IsLoading = true;
        try
        {
            await _handoverService.CreateAsync(NewHandover, SelectedTicketIds.ToList(), CurrentUser.Username);
            IsCreating = false;
            DialogHelper.ShowInfo("تم إنشاء التسليمة بنجاح.");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل إنشاء التسليمة: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AcknowledgeHandoverAsync()
    {
        if (SelectedHandover == null) return;

        try
        {
            await _handoverService.AcknowledgeAsync(SelectedHandover.Id, CurrentUser.Username);
            DialogHelper.ShowInfo("تم تأكيد الاستلام.");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void CancelCreate()
    {
        IsCreating = false;
    }

    partial void OnSelectedHandoverChanged(ShiftHandover? oldValue, ShiftHandover? newValue)
    {
        if (newValue != null)
        {
            _ = LoadHandoverDetailsAsync(newValue.Id);
        }
    }

    private async Task LoadHandoverDetailsAsync(int id)
    {
        try
        {
            var details = await _handoverService.GetByIdAsync(id);
            if (details?.HandoverTickets != null)
            {
                SelectedHandoverTickets = new ObservableCollection<Ticket>(
                    details.HandoverTickets.Select(ht => ht.Ticket));
            }
        }
        catch { }
    }
}
