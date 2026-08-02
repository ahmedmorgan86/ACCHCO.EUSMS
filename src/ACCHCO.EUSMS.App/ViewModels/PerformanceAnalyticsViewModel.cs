using System.IO;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.Reports.Pdf;
using ACCHCO.EUSMS.Reports.Excel;

namespace ACCHCO.EUSMS.App.ViewModels;

public class PerformanceStatItem
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

public class PerformanceKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Icon { get; set; } = "";
    public string ChipColor { get; set; } = "#FFDBEAFE";
    public string AccentColor { get; set; } = "#FF2563EB";
}

public class ShiftSegment
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public double Percent { get; set; }
    public string Color { get; set; } = "#FF94A3B8";
}

public class SpecialistItem
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public int Rank { get; set; }
    public string BadgeColor { get; set; } = "#FFE2E8F0";
}

public partial class PerformanceAnalyticsViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly IDashboardService _dashboardService;

    public PerformanceAnalyticsViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        _ = LoadDataAsync();

        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDataAsync();
    }

    [ObservableProperty] private int _todayTicketCount;
    [ObservableProperty] private int _openTicketCount;
    [ObservableProperty] private int _closedTicketCount;
    [ObservableProperty] private double _averageResolutionMinutes;
    [ObservableProperty] private int _totalEquipment;
    [ObservableProperty] private double _resolutionRate;
    [ObservableProperty] private int _criticalOpenTickets;
    [ObservableProperty] private int _highOpenTickets;
    [ObservableProperty] private ObservableCollection<PerformanceStatItem> _equipmentFaultRates = new();
    [ObservableProperty] private ObservableCollection<PerformanceStatItem> _commonFaults = new();
    [ObservableProperty] private ObservableCollection<KeyValuePair<string, int>> _specialistPerformance = new();
    [ObservableProperty] private ObservableCollection<KeyValuePair<string, int>> _shiftDistribution = new();
    [ObservableProperty] private ObservableCollection<PerformanceKpi> _kpis = new();
    [ObservableProperty] private ObservableCollection<ShiftSegment> _shiftSegments = new();
    [ObservableProperty] private ObservableCollection<SpecialistItem> _specialistLeaderboard = new();
    [ObservableProperty] private double _maxEquipmentFault = 1;
    [ObservableProperty] private double _maxCommonFault = 1;
    [ObservableProperty] private double _maxSpecialist = 1;
    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private IEnumerable<ISeries> _donutSeries = Array.Empty<ISeries>();
    [ObservableProperty] private IEnumerable<ISeries> _faultRateSeries = Array.Empty<ISeries>();
    [ObservableProperty] private IEnumerable<ISeries> _commonFaultSeries = Array.Empty<ISeries>();
    [ObservableProperty] private IEnumerable<ISeries> _specialistSeries = Array.Empty<ISeries>();
    [ObservableProperty] private IEnumerable<ISeries> _shiftSeries = Array.Empty<ISeries>();
    [ObservableProperty] private IEnumerable<ICartesianAxis> _faultXAxis = Array.Empty<ICartesianAxis>();
    [ObservableProperty] private IEnumerable<ICartesianAxis> _commonFaultXAxis = Array.Empty<ICartesianAxis>();
    [ObservableProperty] private IEnumerable<ICartesianAxis> _specialistXAxis = Array.Empty<ICartesianAxis>();
    [ObservableProperty] private IEnumerable<ICartesianAxis> _shiftXAxis = Array.Empty<ICartesianAxis>();
    [ObservableProperty] private IEnumerable<ICartesianAxis> _yAxis = Array.Empty<ICartesianAxis>();

    public ShiftSegment? RedSegment => ShiftSegments.FirstOrDefault(s => s.Label.Contains("حمراء"));
    public ShiftSegment? YellowSegment => ShiftSegments.FirstOrDefault(s => s.Label.Contains("صفراء"));
    public ShiftSegment? BlueSegment => ShiftSegments.FirstOrDefault(s => s.Label.Contains("زرقاء"));

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
            ResolutionRate = kpis.ResolutionRate;
            CriticalOpenTickets = kpis.CriticalOpenTickets;
            HighOpenTickets = kpis.HighOpenTickets;

            var equipmentStats = await _dashboardService.GetFaultsByEquipmentTypeAsync();
            EquipmentFaultRates = new ObservableCollection<PerformanceStatItem>(
                equipmentStats.Select(kvp => new PerformanceStatItem { Name = kvp.Key.ToString(), Count = kvp.Value }));

            var faults = await _dashboardService.GetMostCommonFaultsAsync();
            CommonFaults = new ObservableCollection<PerformanceStatItem>(
                faults.Select(kvp => new PerformanceStatItem { Name = kvp.Key.ToString(), Count = kvp.Value }));

            var bySpecialist = await _dashboardService.GetTicketsBySpecialistAsync();
            SpecialistPerformance = new ObservableCollection<KeyValuePair<string, int>>(
                bySpecialist.Select(kvp => new KeyValuePair<string, int>(kvp.Key, kvp.Value)));

            var byShift = await _dashboardService.GetTicketsByShiftAsync();
            ShiftDistribution = new ObservableCollection<KeyValuePair<string, int>>(
                byShift.Select(kvp => new KeyValuePair<string, int>(kvp.Key.ToString(), kvp.Value)));

            BuildKpis();
            BuildChartSupport();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "فشل تحميل التحليلات");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildKpis()
    {
        Kpis = new ObservableCollection<PerformanceKpi>
        {
            new() { Label = "تذاكر اليوم", Value = TodayTicketCount.ToString(), Icon = "📋", ChipColor = "#FFDBEAFE", AccentColor = "#FF2563EB" },
            new() { Label = "مفتوحة", Value = OpenTicketCount.ToString(), Icon = "📂", ChipColor = "#FFFEF3C7", AccentColor = "#FFD97706" },
            new() { Label = "مغلقة", Value = ClosedTicketCount.ToString(), Icon = "✅", ChipColor = "#FFDCFCE7", AccentColor = "#FF16A34A" },
            new() { Label = "حرجة", Value = CriticalOpenTickets.ToString(), Icon = "🚨", ChipColor = "#FFFEE2E2", AccentColor = "#FFDC2626" },
            new() { Label = "عالية", Value = HighOpenTickets.ToString(), Icon = "🔥", ChipColor = "#FFFFEDD5", AccentColor = "#FFEA580C" },
            new() { Label = "متوسط وقت الحل (د)", Value = AverageResolutionMinutes.ToString("0.0"), Icon = "⏱️", ChipColor = "#FFEDE9FE", AccentColor = "#FF7C3AED" },
            new() { Label = "نسبة الحل", Value = ResolutionRate.ToString("0.0") + "%", Icon = "🎯", ChipColor = "#FFCCFBF1", AccentColor = "#FF0D9488" },
            new() { Label = "إجمالي المعدات", Value = TotalEquipment.ToString(), Icon = "💻", ChipColor = "#FFE0E7FF", AccentColor = "#FF4F46E5" }
        };
    }

    private void BuildChartSupport()
    {
        MaxEquipmentFault = EquipmentFaultRates.Count > 0 ? EquipmentFaultRates.Max(x => x.Count) : 1;
        MaxCommonFault = CommonFaults.Count > 0 ? CommonFaults.Max(x => x.Count) : 1;
        MaxSpecialist = SpecialistPerformance.Count > 0 ? SpecialistPerformance.Max(x => x.Value) : 1;

        var totalShifts = ShiftDistribution.Sum(x => x.Value);
        ShiftSegments = new ObservableCollection<ShiftSegment>(
            ShiftDistribution
                .Select(x => new ShiftSegment
                {
                    Label = x.Key switch
                    {
                        "Red" or "حمراء" => "الوردية الحمراء",
                        "Yellow" or "صفراء" => "الوردية الصفراء",
                        "Blue" or "زرقاء" => "الوردية الزرقاء",
                        _ => x.Key
                    },
                    Count = x.Value,
                    Percent = totalShifts > 0 ? Math.Round((double)x.Value / totalShifts * 100, 1) : 0,
                    Color = x.Key switch
                    {
                        "Red" or "حمراء" => "#FFEF4444",
                        "Yellow" or "صفراء" => "#FFF59E0B",
                        "Blue" or "زرقاء" => "#FF3B82F6",
                        _ => "#FF94A3B8"
                    }
                }));

        SpecialistLeaderboard = new ObservableCollection<SpecialistItem>(
            SpecialistPerformance
                .OrderByDescending(x => x.Value)
                .Select((x, i) => new SpecialistItem
                {
                    Name = x.Key,
                    Count = x.Value,
                    Rank = i + 1,
                    BadgeColor = i switch
                    {
                        0 => "#FFFBBF24",
                        1 => "#FFCBD5E1",
                        2 => "#FFD4A017",
                        _ => "#FFE2E8F0"
                    }
                }));

        BuildLiveChartsData();

        OnPropertyChanged(nameof(RedSegment));
        OnPropertyChanged(nameof(YellowSegment));
        OnPropertyChanged(nameof(BlueSegment));
    }

    private void BuildLiveChartsData()
    {
        YAxis = new ICartesianAxis[]
        {
            new Axis
            {
                Position = AxisPosition.End,
                MinLimit = 0,
                TextSize = 13,
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("E2E8F0")) { StrokeThickness = 1 }
            }
        };

        DonutSeries = new ISeries[]
        {
            new PieSeries<double>
            {
                Name = "مغلقة",
                Values = new double[] { ClosedTicketCount },
                Fill = new SolidColorPaint(SKColor.Parse("16A34A")),
                Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 4 },
                RelativeInnerRadius = 0.72,
                HoverPushout = 6
            },
            new PieSeries<double>
            {
                Name = "مفتوحة",
                Values = new double[] { OpenTicketCount },
                Fill = new SolidColorPaint(SKColor.Parse("F59E0B")),
                Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 4 },
                RelativeInnerRadius = 0.72,
                HoverPushout = 6
            }
        };

        var faultRates = EquipmentFaultRates.OrderBy(x => x.Count).ToArray();
        FaultRateSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "عدد الأعطال",
                Values = faultRates.Select(x => (double)x.Count).ToArray(),
                Fill = new SolidColorPaint(SKColor.Parse("2563EB")),
                MaxBarWidth = 34,
                Rx = 6
            }
        };
        FaultXAxis = new ICartesianAxis[] { CreateCategoryAxis(faultRates.Select(x => x.Name)) };

        var commonFaults = CommonFaults.OrderBy(x => x.Count).ToArray();
        CommonFaultSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "مرات التكرار",
                Values = commonFaults.Select(x => (double)x.Count).ToArray(),
                Fill = new SolidColorPaint(SKColor.Parse("F59E0B")),
                MaxBarWidth = 34,
                Rx = 6
            }
        };
        CommonFaultXAxis = new ICartesianAxis[] { CreateCategoryAxis(commonFaults.Select(x => x.Name)) };

        var specialists = SpecialistPerformance.OrderBy(x => x.Value).ToArray();
        SpecialistSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "تذاكر",
                Values = specialists.Select(x => (double)x.Value).ToArray(),
                Fill = new SolidColorPaint(SKColor.Parse("0D9488")),
                MaxBarWidth = 34,
                Rx = 6
            }
        };
        SpecialistXAxis = new ICartesianAxis[]
        {
            CreateCategoryAxis(specialists.Select(x => x.Key))
        };

        var shifts = new[] { RedSegment, YellowSegment, BlueSegment }.OfType<ShiftSegment>().Reverse().ToList();
        ShiftSeries = shifts
            .Select(s => new ColumnSeries<double>
            {
                Name = s.Label,
                Values = new double[] { s.Count },
                Fill = new SolidColorPaint(SKColor.Parse(s.Color.Substring(3))),
                MaxBarWidth = 46,
                Rx = 6
            })
            .ToList();
        ShiftXAxis = new ICartesianAxis[] { CreateCategoryAxis(shifts.Select(s => s.Label)) };
    }

    private static Axis CreateCategoryAxis(IEnumerable<string> labels) => new()
    {
        Labels = labels.ToArray(),
        TextSize = 13,
        LabelsPaint = new SolidColorPaint(SKColors.Black),
        SeparatorsPaint = new SolidColorPaint(SKColor.Parse("E2E8F0")) { StrokeThickness = 1 }
    };

    [RelayCommand]
    private void ExportPdf(string sectionTitle) => ExportSection(sectionTitle, isExcel: false);

    [RelayCommand]
    private void ExportExcel(string sectionTitle) => ExportSection(sectionTitle, isExcel: true);

    private void ExportSection(string sectionTitle, bool isExcel)
    {
        try
        {
            var displayTitle = sectionTitle.Replace('_', ' ');

            var rows = sectionTitle switch
            {
                "معدلات_أعطال_المعدات" => EquipmentFaultRates.Select(x => new KeyValuePair<string, string>(x.Name, x.Count.ToString())),
                "الأعطال_الأكثر_شيوعا" => CommonFaults.Select(x => new KeyValuePair<string, string>(x.Name, x.Count.ToString())),
                "أداء_الأخصائيين" => SpecialistPerformance.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())),
                "توزيع_التذاكر_حسب_الوردية" => ShiftDistribution.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())),
                _ => Enumerable.Empty<KeyValuePair<string, string>>()
            };

            var kpis = new List<KeyValuePair<string, string>>
            {
                new("تذاكر اليوم", TodayTicketCount.ToString()),
                new("مفتوحة", OpenTicketCount.ToString()),
                new("مغلقة", ClosedTicketCount.ToString()),
                new("حرجة", CriticalOpenTickets.ToString()),
                new("عالية", HighOpenTickets.ToString()),
                new("متوسط وقت الحل (د)", AverageResolutionMinutes.ToString("0.0")),
                new("نسبة الحل %", ResolutionRate.ToString("0.0")),
                new("إجمالي المعدات", TotalEquipment.ToString())
            };

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = isExcel ? "Excel Files (*.xlsx)|*.xlsx" : "PDF Files (*.pdf)|*.pdf",
                FileName = $"تقرير_{sectionTitle}_{DateTime.Now:yyyyMMdd}{(isExcel ? ".xlsx" : ".pdf")}"
            };

            if (dialog.ShowDialog() != true) return;

            byte[] bytes;
            if (isExcel)
            {
                var excelGen = App.ServiceProvider!.GetRequiredService<ExcelReportGenerator>();
                bytes = excelGen.GeneratePerformanceSectionReport(displayTitle, kpis, rows);
            }
            else
            {
                var pdfGen = App.ServiceProvider!.GetRequiredService<PdfReportGenerator>();
                bytes = pdfGen.GeneratePerformanceSectionReport(displayTitle, kpis, rows);
            }

            File.WriteAllBytes(dialog.FileName, bytes);
            DialogHelper.ShowInfo($"تم حفظ التقرير '{displayTitle}' بنجاح في:\n{dialog.FileName}");
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل تصدير التقرير: {ex.Message}");
        }
    }
}
