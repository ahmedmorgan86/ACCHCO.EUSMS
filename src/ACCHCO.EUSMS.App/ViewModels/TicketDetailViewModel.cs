using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.App.Helpers;
using Serilog;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class TicketDetailViewModel : ObservableObject
{
    private readonly ITicketService _ticketService;
    private readonly IAppUserService _userService;
    private readonly IEquipmentService _equipmentService;

    public TicketDetailViewModel()
    {
        _ticketService = App.ServiceProvider!.GetRequiredService<ITicketService>();
        _userService = App.ServiceProvider!.GetRequiredService<IAppUserService>();
        _equipmentService = App.ServiceProvider!.GetRequiredService<IEquipmentService>();
        SetDefaultValues();
        _ = LoadRequesterInfoAsync();
        _ = LoadLookupsAsync();
    }

    public event Action? RequestClose;

    private async Task LoadRequesterInfoAsync()
    {
        try
        {
            var adService = App.ServiceProvider!.GetRequiredService<IActiveDirectoryService>();
            var adUser = await adService.FindUserByUsernameAsync(CurrentUser.Username);
            if (adUser != null)
            {
                Ticket.RequesterName = !string.IsNullOrWhiteSpace(adUser.DisplayName) ? adUser.DisplayName : CurrentUser.Username;
                Ticket.RequesterSection = adUser.Department ?? adUser.Office ?? string.Empty;
                Ticket.RequesterEmail = adUser.Email ?? string.Empty;
                Ticket.RequesterPhone = adUser.Phone ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(adUser.DisplayName))
                {
                    CurrentUser.FullName = adUser.DisplayName;
                }
                return;
            }

            var user = await _userService.GetByUsernameAsync(CurrentUser.Username);
            if (user != null)
            {
                Ticket.RequesterName = user.FullName;
                Ticket.RequesterSection = user.Section ?? string.Empty;
                Ticket.RequesterEmail = user.Email ?? string.Empty;
                Ticket.RequesterPhone = user.Phone ?? string.Empty;
            }
            else
            {
                Ticket.RequesterName = CurrentUser.FullName ?? CurrentUser.Username;
            }
        }
        catch
        {
            Ticket.RequesterName = CurrentUser.FullName ?? CurrentUser.Username;
        }
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            var specialists = await _userService.GetActiveSpecialistsAsync();
            Specialists = new ObservableCollection<AppUser>(specialists);

            var equipment = await _equipmentService.GetAllAsync();
            EquipmentList = new ObservableCollection<Equipment>(equipment.OrderBy(e => e.Name));

            if (IsNewTicket)
                GeneratedTicketNumber = await _ticketService.PeekNextNumberAsync();
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل تحميل البيانات: {ex.Message}");
        }
    }

    private void SetDefaultValues()
    {
        Ticket.TicketDate = DateTime.Today;
        Ticket.TicketTime = DateTime.Now.TimeOfDay;
        Ticket.Status = TicketStatus.Open;
        Ticket.Priority = Priority.Medium;
        Ticket.Shift = Shift.Red;
        Ticket.SupportSpecialistId = CurrentUser.UserId;
    }

    public void InitializeForDevice(NetworkDevice device)
    {
        if (!string.IsNullOrWhiteSpace(device.Hostname))
            Ticket.EquipmentName = device.Hostname;
        if (!string.IsNullOrWhiteSpace(device.IpAddress))
            Ticket.IpAddress = device.IpAddress;
        Ticket.Location = device.DeviceCategory;
    }

    public void InitializeForEquipment(Equipment equipment)
    {
        Ticket.EquipmentType = equipment.Type;
        Ticket.EquipmentName = equipment.Name;
        Ticket.AssetTag = equipment.AssetTag ?? Ticket.AssetTag;
        Ticket.ComputerName = equipment.ComputerName ?? Ticket.ComputerName;
        Ticket.IpAddress = equipment.IpAddress ?? Ticket.IpAddress;
        Ticket.OperatingSystem = equipment.OperatingSystem ?? Ticket.OperatingSystem;
        Ticket.Location = equipment.Location ?? Ticket.Location;
        SelectEquipment(equipment.Name);
    }

    public void SelectEquipment(string? equipmentName)
    {
        if (!string.IsNullOrWhiteSpace(equipmentName))
        {
            var match = EquipmentList.FirstOrDefault(e => e.Name == equipmentName);
            if (match != null)
                SelectedEquipment = match;
        }
    }

    [ObservableProperty] private Ticket _ticket = new();
    [ObservableProperty] private bool _isNewTicket = true;
    [ObservableProperty] private bool _isEditMode = true;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _newSolution = string.Empty;
    [ObservableProperty] private string _generatedTicketNumber = string.Empty;
    [ObservableProperty] private ObservableCollection<AppUser> _specialists = new();
    [ObservableProperty] private ObservableCollection<Equipment> _equipmentList = new();
    [ObservableProperty] private Equipment? _selectedEquipment;

    partial void OnSelectedEquipmentChanged(Equipment? value)
    {
        if (value == null || IsNewTicket == false && IsEditMode == false)
        {
            if (value != null) { /* allow set */ }
            return;
        }

        Ticket.EquipmentType = value.Type;
        Ticket.EquipmentName = value.Name;
        Ticket.AssetTag = value.AssetTag ?? Ticket.AssetTag;
        Ticket.ComputerName = value.ComputerName ?? Ticket.ComputerName;
        Ticket.IpAddress = value.IpAddress ?? Ticket.IpAddress;
        Ticket.OperatingSystem = value.OperatingSystem ?? Ticket.OperatingSystem;
        Ticket.Location = value.Location ?? Ticket.Location;
    }

    public string WindowTitle => IsNewTicket ? "تذكرة جديدة" : $"تذكرة رقم {Ticket.TicketNumber}";

    [RelayCommand]
    private async Task LoadTicketAsync(int ticketId)
    {
        IsLoading = true;
        try
        {
            var ticket = await _ticketService.GetByIdAsync(ticketId);
            if (ticket != null)
            {
                Ticket = ticket;
                IsNewTicket = false;
                IsEditMode = false;
            }
            await LoadLookupsAsync();
            SelectEquipment(Ticket.EquipmentName);
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل تحميل التذكرة: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveTicketAsync()
    {
        if (string.IsNullOrWhiteSpace(Ticket.ProblemDescription))
        {
            DialogHelper.ShowWarning("وصف المشكلة مطلوب.");
            return;
        }

        IsLoading = true;
        try
        {
            if (IsNewTicket)
            {
                Log.Information("Saving New Ticket. SpecialistID: {Id}", Ticket.SupportSpecialistId);
                var created = await _ticketService.CreateAsync(Ticket, CurrentUser.Username);
                Ticket = created;
                OnPropertyChanged(nameof(Ticket));
                IsNewTicket = false;
                IsEditMode = false;
                WeakReferenceMessenger.Default.Send(new ShowSnackbarMessage($"تم إنشاء التذكرة {created.TicketNumber} بنجاح."));
                RequestClose?.Invoke();
            }
            else
            {
                Log.Information("Updating Ticket {Id}. SpecialistID: {SpecialistId}", Ticket.Id, Ticket.SupportSpecialistId);
                await _ticketService.UpdateAsync(Ticket, CurrentUser.Username);
                IsEditMode = false;
                WeakReferenceMessenger.Default.Send(new ShowSnackbarMessage("تم تحديث التذكرة بنجاح."));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "فشل حفظ التذكرة");
            DialogHelper.ShowError($"فشل حفظ التذكرة: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleEdit()
    {
        IsEditMode = !IsEditMode;
        if (IsEditMode && !IsNewTicket)
            SelectEquipment(Ticket.EquipmentName);
    }

    [RelayCommand]
    private async Task ResolveTicketAsync()
    {
        if (string.IsNullOrWhiteSpace(NewSolution))
        {
            DialogHelper.ShowWarning("يرجى إدخال الحل.");
            return;
        }

        var confirmed = DialogHelper.Confirm("هل تريد حل هذه التذكرة؟", "تأكيد الحل");
        if (!confirmed) return;

        IsLoading = true;
        try
        {
            await _ticketService.ResolveAsync(Ticket.Id, NewSolution, CurrentUser.Username);
            var updated = await _ticketService.GetByIdAsync(Ticket.Id);
            if (updated != null) Ticket = updated;
            NewSolution = string.Empty;
            IsEditMode = false;
            DialogHelper.ShowInfo("تم حل التذكرة بنجاح.");
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل حل التذكرة: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CloseTicketAsync()
    {
        var confirmed = DialogHelper.Confirm("هل تريد إغلاق هذه التذكرة؟", "تأكيد الإغلاق");
        if (!confirmed) return;

        IsLoading = true;
        try
        {
            await _ticketService.CloseAsync(Ticket.Id, CurrentUser.Username);
            var updated = await _ticketService.GetByIdAsync(Ticket.Id);
            if (updated != null) Ticket = updated;
            IsEditMode = false;
            DialogHelper.ShowInfo("تم إغلاق التذكرة.");
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل إغلاق التذكرة: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CloseDialog()
    {
        RequestClose?.Invoke();
    }
}
