using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.Views;
using Microsoft.EntityFrameworkCore;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class DeviceReplacementsViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IRepository<DeviceReplacement> _replacementRepo;

    public DeviceReplacementsViewModel()
    {
        _replacementRepo = App.ServiceProvider!.GetRequiredService<IRepository<DeviceReplacement>>();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<ReplacementDisplay> _replacementsList = new();
    [ObservableProperty] private ReplacementDisplay? _selectedReplacement;
    [ObservableProperty] private bool _isLoading;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var all = await _replacementRepo.Query()
                .AsNoTracking()
                .Include(r => r.OldEquipment)
                .Include(r => r.NewEquipment)
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.ReplacementDate)
                .ToListAsync();

            ReplacementsList = new ObservableCollection<ReplacementDisplay>(
                all.Select(r => new ReplacementDisplay
                {
                    Id = r.Id,
                    OldEquipmentName = r.OldEquipment?.Name ?? "غير معروف",
                    NewEquipmentName = r.NewEquipment?.Name ?? "غير معروف",
                    ReplacementDate = r.ReplacementDate,
                    Reason = r.Reason
                }));
            Serilog.Log.Information("Loaded {Count} device replacements", all.Count);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "فشل تحميل الاستبدالات");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddNew()
    {
        var window = new ReplacementDetailWindow();
        if (window.ShowDialog() == true)
            _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (SelectedReplacement == null)
        {
            DialogHelper.ShowWarning("اختر سجل استبدال أولاً.");
            return;
        }

        try
        {
            var entity = await _replacementRepo.GetByIdAsync(SelectedReplacement.Id);
            if (entity == null)
            {
                DialogHelper.ShowError("لم يتم العثور على السجل.");
                return;
            }

            var window = new ReplacementDetailWindow(entity);
            if (window.ShowDialog() == true)
                await LoadDataAsync();
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل التعديل: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedReplacement == null)
        {
            DialogHelper.ShowWarning("اختر سجل استبدال أولاً.");
            return;
        }

        if (!DialogHelper.Confirm($"هل تريد حذف سجل استبدال ('{SelectedReplacement.OldEquipmentName}' ← '{SelectedReplacement.NewEquipmentName}')؟", "تأكيد الحذف"))
            return;

        try
        {
            var entity = await _replacementRepo.GetByIdAsync(SelectedReplacement.Id);
            if (entity != null)
                await _replacementRepo.SoftDeleteAsync(entity);
            DialogHelper.ShowInfo("تم حذف سجل الاستبدال.");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل الحذف: {ex.Message}");
        }
    }
}

public class ReplacementDisplay
{
    public int Id { get; set; }
    public string OldEquipmentName { get; set; } = "";
    public string NewEquipmentName { get; set; } = "";
    public DateTime ReplacementDate { get; set; }
    public string Reason { get; set; } = "";
}
