using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class AuditLogViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IAuditService _auditService;
    private readonly ACCHCO.EUSMS.Reports.Excel.ExcelReportGenerator _excelGenerator;

    public AuditLogViewModel()
    {
        _auditService = App.ServiceProvider!.GetRequiredService<IAuditService>();
        _excelGenerator = App.ServiceProvider!.GetRequiredService<ACCHCO.EUSMS.Reports.Excel.ExcelReportGenerator>();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private ObservableCollection<AuditLog> _logs = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private DateTime? _filterFrom = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime? _filterTo = DateTime.Today;
    [ObservableProperty] private string _filterUsername = string.Empty;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var logs = await _auditService.GetAllAsync();
            Logs = new ObservableCollection<AuditLog>(logs);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "فشل تحميل سجل المراجعة");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task FilterAsync()
    {
        IsLoading = true;
        try
        {
            if (!string.IsNullOrWhiteSpace(FilterUsername))
            {
                var logs = await _auditService.GetByUserAsync(FilterUsername);
                Logs = new ObservableCollection<AuditLog>(logs);
            }
            else if (FilterFrom.HasValue && FilterTo.HasValue)
            {
                var logs = await _auditService.GetByDateRangeAsync(FilterFrom.Value, FilterTo.Value.AddDays(1));
                Logs = new ObservableCollection<AuditLog>(logs);
            }
            else
            {
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (!Logs.Any())
        {
            DialogHelper.ShowWarning("لا توجد بيانات للتصدير.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"EUSMS_AuditLog_{DateTime.Now:yyyyMMdd}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            var data = _excelGenerator.GenerateAuditReport(Logs);
            File.WriteAllBytes(dialog.FileName, data);
            DialogHelper.ShowInfo($"تم التصدير إلى {dialog.FileName}");
        }
    }
}
