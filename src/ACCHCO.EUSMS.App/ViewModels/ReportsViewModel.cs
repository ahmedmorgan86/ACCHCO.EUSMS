using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly ITicketService _ticketService;
    private readonly IEquipmentService _equipmentService;
    private readonly IAuditService _auditService;
    private readonly ACCHCO.EUSMS.Reports.Pdf.PdfReportGenerator _pdfGenerator;
    private readonly ACCHCO.EUSMS.Reports.Excel.ExcelReportGenerator _excelGenerator;
    private readonly ACCHCO.EUSMS.Reports.Csv.CsvReportGenerator _csvGenerator;

    public ReportsViewModel()
    {
        _ticketService = App.ServiceProvider!.GetRequiredService<ITicketService>();
        _equipmentService = App.ServiceProvider!.GetRequiredService<IEquipmentService>();
        _auditService = App.ServiceProvider!.GetRequiredService<IAuditService>();
        _pdfGenerator = App.ServiceProvider!.GetRequiredService<ACCHCO.EUSMS.Reports.Pdf.PdfReportGenerator>();
        _excelGenerator = App.ServiceProvider!.GetRequiredService<ACCHCO.EUSMS.Reports.Excel.ExcelReportGenerator>();
        _csvGenerator = App.ServiceProvider!.GetRequiredService<ACCHCO.EUSMS.Reports.Csv.CsvReportGenerator>();
    }

    [ObservableProperty] private ObservableCollection<Ticket> _reportTickets = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _reportTitle = "Ticket Report";

    [ObservableProperty] private DateTime? _reportDateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _reportDateTo = DateTime.Today;
    [ObservableProperty] private Shift? _reportShift;
    [ObservableProperty] private int? _reportSpecialistId;
    [ObservableProperty] private TicketStatus? _reportStatus;
    [ObservableProperty] private Priority? _reportPriority;
    [ObservableProperty] private EquipmentType? _reportEquipmentType;
    [ObservableProperty] private FaultType? _reportFaultType;

    [ObservableProperty] private ObservableCollection<AppUser> _specialists = new();

    [RelayCommand]
    private async Task LoadSpecialistsAsync()
    {
        var userService = App.ServiceProvider!.GetRequiredService<IAppUserService>();
        var specialists = await userService.GetActiveSpecialistsAsync();
        Specialists = new ObservableCollection<AppUser>(specialists);
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        IsLoading = true;
        try
        {
            var tickets = await _ticketService.GetFilteredAsync(
                ReportDateFrom, ReportDateTo, ReportShift, ReportSpecialistId,
                null, ReportEquipmentType, ReportFaultType, ReportPriority, ReportStatus);
            ReportTickets = new ObservableCollection<Ticket>(tickets);
            ReportTitle = $"Ticket Report ({ReportDateFrom:dd/MM/yyyy} - {ReportDateTo:dd/MM/yyyy})";
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل إنشاء التقرير: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ExportToPdf()
    {
        if (!ReportTickets.Any())
        {
            DialogHelper.ShowWarning("قم بإنشاء التقرير أولاً.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PDF Files|*.pdf",
            FileName = $"EUSMS_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var data = _pdfGenerator.GenerateTicketReport(ReportTickets, ReportTitle, DateTime.Now);
                File.WriteAllBytes(dialog.FileName, data);
                DialogHelper.ShowInfo($"تم تصدير التقرير إلى:\n{dialog.FileName}");
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"فشل تصدير التقرير إلى PDF: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (!ReportTickets.Any())
        {
            DialogHelper.ShowWarning("قم بإنشاء التقرير أولاً.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"EUSMS_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var data = _excelGenerator.GenerateTicketReport(ReportTickets, ReportTitle);
                File.WriteAllBytes(dialog.FileName, data);
                DialogHelper.ShowInfo($"تم تصدير التقرير إلى:\n{dialog.FileName}");
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"فشل تصدير التقرير إلى Excel: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void ExportToCsv()
    {
        if (!ReportTickets.Any())
        {
            DialogHelper.ShowWarning("قم بإنشاء التقرير أولاً.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV Files|*.csv",
            FileName = $"EUSMS_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var csv = _csvGenerator.GenerateTicketReport(ReportTickets);
                var data = _csvGenerator.ToBytes(csv);
                File.WriteAllBytes(dialog.FileName, data);
                DialogHelper.ShowInfo($"تم تصدير التقرير إلى:\n{dialog.FileName}");
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"فشل تصدير التقرير إلى CSV: {ex.Message}");
            }
        }
    }
}
