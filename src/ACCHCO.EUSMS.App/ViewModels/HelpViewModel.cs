using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.Reports.Pdf;

namespace ACCHCO.EUSMS.App.ViewModels;

public class HelpFeature
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ChipColor { get; set; } = "#FFDBEAFE";
    public string AccentColor { get; set; } = "#FF2563EB";
}

public class HelpStep
{
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
}

public partial class HelpViewModel : ObservableObject
{
    private readonly PdfReportGenerator _pdfGenerator;

    public HelpViewModel(PdfReportGenerator pdfGenerator)
    {
        _pdfGenerator = pdfGenerator;
        Features = new ObservableCollection<HelpFeature>
        {
            new() { Icon = "🖥️", Title = "لوحة التحكم", Description = "مؤشرات الأداء الرئيسية وأحدث التذاكر والورديات", ChipColor = "#FFDBEAFE", AccentColor = "#FF2563EB" },
            new() { Icon = "🎫", Title = "التذاكر", Description = "إنشاء ومتابعة وحل تذاكر الدعم التقني", ChipColor = "#FFDCFCE7", AccentColor = "#FF16A34A" },
            new() { Icon = "🌐", Title = "أجهزة الشبكة", Description = "مسح ذكي لاكتشاف أجهزة شبكات الشركة", ChipColor = "#FFE0E7FF", AccentColor = "#FF4F46E5" },
            new() { Icon = "🔄", Title = "استبدال الأجهزة", Description = "توثيق عمليات الاستبدال وسجلها الكامل", ChipColor = "#FFFEF3C7", AccentColor = "#FFD97706" },
            new() { Icon = "📋", Title = "تسليم الورديات", Description = "توثيق التسليم بين الورديات الحمراء والصفراء والزرقاء", ChipColor = "#FFFFEDD5", AccentColor = "#FFEA580C" },
            new() { Icon = "⚙️", Title = "الإعدادات", Description = "الإعدادات العامة والمزامنة مع Active Directory", ChipColor = "#FFEDE9FE", AccentColor = "#FF7C3AED" }
        };
        Steps = new ObservableCollection<HelpStep>
        {
            new() { Number = 1, Icon = "🆕", Title = "أنشئ تذكرة", Description = "اضغط زر 'إضافة تذكرة' وعبّئ بيانات العطل والمعدة" },
            new() { Number = 2, Icon = "🔎", Title = "تتبع الحالة", Description = "تابع حالة التذكرة من لوحة التحكم أو صفحة التذاكر" },
            new() { Number = 3, Icon = "✅", Title = "حل وأغلق", Description = "سجّل إجراءات الحل وأغلق التذكرة بعد انتهاء العمل" },
            new() { Number = 4, Icon = "📊", Title = "راجع التحليلات", Description = "تصفح تقارير الأداء والمعدات وصدّرها PDF أو Excel" }
        };
    }

    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private Visibility _loadingVisibility = Visibility.Collapsed;
    [ObservableProperty] private bool _canDownload = true;
    [ObservableProperty] private ObservableCollection<HelpFeature> _features;
    [ObservableProperty] private ObservableCollection<HelpStep> _steps;

    public string AppVersion
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v == null ? "1.0" : $"الإصدار {v.Major}.{v.Minor}.{v.Build}";
        }
    }

    [RelayCommand]
    private async Task DownloadPdfAsync()
    {
        IsGenerating = true;
        LoadingVisibility = Visibility.Visible;
        CanDownload = false;
        try
        {
            var pdfBytes = _pdfGenerator.GenerateUserManualPdf();
            var savePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"دليل_استخدام_EUSMS_{DateTime.Now:yyyy-MM-dd}.pdf");
            await File.WriteAllBytesAsync(savePath, pdfBytes);
            DialogHelper.ShowInfo($"تم حفظ الدليل على سطح المكتب:\n{savePath}");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = savePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل إنشاء ملف PDF: {ex.Message}");
        }
        finally
        {
            IsGenerating = false;
            LoadingVisibility = Visibility.Collapsed;
            CanDownload = true;
        }
    }
}
