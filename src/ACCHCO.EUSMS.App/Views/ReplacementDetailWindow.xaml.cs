using System.Windows;
using Serilog;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;

namespace ACCHCO.EUSMS.App.Views;

public partial class ReplacementDetailWindow : Window
{
    private readonly IRepository<Equipment> _equipmentRepo;
    private readonly IRepository<DeviceReplacement> _replacementRepo;
    private readonly DeviceReplacement? _existing;

    public ReplacementDetailWindow() : this(null) { }

    public ReplacementDetailWindow(DeviceReplacement? existing)
    {
        InitializeComponent();
        _existing = existing;
        _equipmentRepo = App.ServiceProvider!.GetRequiredService<IRepository<Equipment>>();
        _replacementRepo = App.ServiceProvider!.GetRequiredService<IRepository<DeviceReplacement>>();
        Loaded += async (s, e) => await LoadEquipmentAsync();
        ReplacementDatePicker.SelectedDate = DateTime.Today;

        if (_existing != null)
        {
            Title = "تعديل الاستبدال";
            TitleTextBlock.Text = "تعديل الاستبدال";
            ReplacementDatePicker.SelectedDate = _existing.ReplacementDate;
            ReasonTextBox.Text = _existing.Reason;
        }
    }

    private async Task LoadEquipmentAsync()
    {
        var all = (await _equipmentRepo.GetAllAsync()).OrderBy(e => e.Name).ToList();
        OldEquipmentCombo.ItemsSource = all;
        NewEquipmentCombo.ItemsSource = all;

        if (_existing != null)
        {
            OldEquipmentCombo.SelectedItem = all.FirstOrDefault(e => e.Id == _existing.OldEquipmentId);
            NewEquipmentCombo.SelectedItem = all.FirstOrDefault(e => e.Id == _existing.NewEquipmentId);
        }
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        var oldEquip = OldEquipmentCombo.SelectedItem as Equipment;
        var newEquip = NewEquipmentCombo.SelectedItem as Equipment;
        var reason = ReasonTextBox.Text.Trim();
        var date = ReplacementDatePicker.SelectedDate ?? DateTime.Today;

        if (oldEquip == null || newEquip == null)
        {
            MessageBox.Show("يرجى اختيار الجهاز القديم والجديد.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            MessageBox.Show("يرجى إدخال سبب الاستبدال.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (oldEquip.Id == newEquip.Id)
        {
            MessageBox.Show("يجب أن يكون الجهاز القديم والجديد مختلفين.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_existing != null)
            {
                _existing.OldEquipmentId = oldEquip.Id;
                _existing.NewEquipmentId = newEquip.Id;
                _existing.Reason = reason;
                _existing.ReplacementDate = date;
                await _replacementRepo.UpdateAsync(_existing);
                Log.Information("Replacement updated: Id={Id} Old={OldId}({OldName}) New={NewId}({NewName}) By={User}",
                    _existing.Id, oldEquip.Id, oldEquip.Name, newEquip.Id, newEquip.Name, ACCHCO.EUSMS.App.Helpers.CurrentUser.Username);
            }
            else
            {
                await _replacementRepo.AddAsync(new DeviceReplacement
                {
                    OldEquipmentId = oldEquip.Id,
                    NewEquipmentId = newEquip.Id,
                    Reason = reason,
                    ReplacementDate = date,
                    CreatedBy = ACCHCO.EUSMS.App.Helpers.CurrentUser.Username
                });
                Log.Information("Replacement saved: Old={OldId}({OldName}) New={NewId}({NewName}) Date={Date:yyyy-MM-dd} By={User}",
                    oldEquip.Id, oldEquip.Name, newEquip.Id, newEquip.Name, date, ACCHCO.EUSMS.App.Helpers.CurrentUser.Username);
            }
            var count = await _replacementRepo.CountAsync();
            Log.Information("DeviceReplacements total count after save: {Count}", count);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
