using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingService _settingService;
    private readonly IActiveDirectoryService _adService;

    private static readonly Dictionary<string, string> KeyTranslations = new()
    {
        ["CompanyName"] = "اسم الشركة",
        ["CompanyNameAr"] = "اسم الشركة (بالعربية)",
        ["DepartmentName"] = "اسم القسم",
        ["AppTitle"] = "عنوان التطبيق",
        ["TicketPrefix"] = "بادئة رقم التذكرة",
        ["DefaultPriority"] = "الأولوية الافتراضية للتذاكر",
        ["DefaultShift"] = "الوردية الافتراضية",
        ["AdSyncEnabled"] = "تفعيل مزامنة Active Directory",
        ["AdDomain"] = "نطاق Active Directory",
        ["AdOuPath"] = "مسار الوحدة التنظيمية (OU)",
        ["BackupPath"] = "مسار النسخ الاحتياطي",
        ["MaxFileSize"] = "الحد الأقصى لحجم رفع الملفات (MB)",
        ["ShowDashboardWidgets"] = "إظهار أدوات لوحة المعلومات",
        ["PrimaryColor"] = "اللون الأساسي للواجهة",
        ["AccentColor"] = "لون التمييز",
    };

    private static readonly Dictionary<string, string> DescTranslations = new()
    {
        ["Company Name"] = "اسم الشركة الظاهر في التطبيق",
        ["Company Name (Arabic)"] = "اسم الشركة باللغة العربية",
        ["Department Name"] = "اسم القسم المسؤول عن الدعم",
        ["Application Title"] = "عنوان التطبيق الظاهر في شاشة البداية",
        ["Ticket Number Prefix"] = "اختصار يسبق رقم التذكرة",
        ["Default Ticket Priority"] = "الأولوية الافتراضية للتذاكر الجديدة",
        ["Default Shift"] = "الوردية الافتراضية للتذاكر الجديدة",
        ["Enable AD Synchronization"] = "تفعيل المزامنة التلقائية مع Active Directory",
        ["AD Domain Name"] = "اسم نطاق Active Directory الخاص بالشركة",
        ["AD OU Path (leave empty for all)"] = "مسار الوحدة التنظيمية (اترك فارغاً للكل)",
        ["Backup Directory Path"] = "مسار المجلد الذي سيتم حفظ النسخ الاحتياطية فيه",
        ["Max Upload File Size (MB)"] = "الحد الأقصى لحجم الملفات المرفوعة بالميجابايت",
        ["Show Dashboard Widgets"] = "إظهار/إخفاء أدوات لوحة المعلومات الرئيسية",
        ["Primary Theme Color"] = "اللون الأساسي لشريط التنقل والعناوين",
        ["Accent Theme Color"] = "لون التمييز للأزرار والعناصر التفاعلية",
    };

    private static readonly Dictionary<string, string> GroupTranslations = new()
    {
        ["General"] = "عام",
        ["Tickets"] = "التذاكر",
        ["Active Directory"] = "الدليل النشط (Active Directory)",
        ["Backup"] = "النسخ الاحتياطي",
        ["Dashboard"] = "لوحة المعلومات",
        ["Appearance"] = "المظهر",
    };

    public SettingsViewModel()
    {
        _settingService = App.ServiceProvider!.GetRequiredService<ISettingService>();
        _adService = App.ServiceProvider!.GetRequiredService<IActiveDirectoryService>();
    }

    [ObservableProperty] private ObservableCollection<SettingsGroup> _settingsGroups = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isAdAvailable;
    [ObservableProperty] private int _adComputerCount;
    [ObservableProperty] private int _adUserCount;
    [ObservableProperty] private DateTime? _lastComputerSync;
    [ObservableProperty] private DateTime? _lastUserSync;
    [ObservableProperty] private bool _isSyncing;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var allSettings = await _settingService.GetAllEntitiesAsync();
            var groups = allSettings
                .GroupBy(s => GroupTranslations.GetValueOrDefault(s.GroupName ?? "", s.GroupName ?? ""))
                .Select(g => new SettingsGroup
                {
                    GroupName = g.Key,
                    Settings = new ObservableCollection<SettingItem>(
                        g.Select(s => new SettingItem
                        {
                            Key = s.Key,
                            DisplayKey = KeyTranslations.GetValueOrDefault(s.Key, s.Key),
                            Value = s.Value,
                            Description = DescTranslations.GetValueOrDefault(s.Description ?? "", s.Description ?? "")
                        }))
                });
            SettingsGroups = new ObservableCollection<SettingsGroup>(groups);

            IsAdAvailable = _adService.IsAvailable();
            if (IsAdAvailable)
            {
                AdComputerCount = await _adService.GetComputerCountAsync();
                AdUserCount = await _adService.GetUserCountAsync();
                LastComputerSync = _adService.GetLastComputerSyncDate();
                LastUserSync = _adService.GetLastUserSyncDate();
            }
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل تحميل الإعدادات: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CheckAd()
    {
        IsAdAvailable = _adService.IsAvailable();
        if (IsAdAvailable)
        {
            DialogHelper.ShowInfo("تم الاتصال بـ Active Directory بنجاح.");
        }
        else
        {
            DialogHelper.ShowWarning("تعذر الاتصال بـ Active Directory.\nتأكد من اتصالك بالشبكة وصلاحية الحساب.");
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        IsLoading = true;
        try
        {
            foreach (var group in SettingsGroups)
            {
                foreach (var setting in group.Settings)
                {
                    await _settingService.SetAsync(setting.Key, setting.Value,
                        setting.Description, group.GroupName);
                }
            }
            DialogHelper.ShowInfo("تم حفظ الإعدادات بنجاح.");
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل الحفظ: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SyncAdComputersAsync()
    {
        IsSyncing = true;
        try
        {
            var computers = await _adService.SyncComputersAsync();
            AdComputerCount = computers.Count();
            LastComputerSync = _adService.GetLastComputerSyncDate();
            DialogHelper.ShowInfo($"تم مزامنة {computers.Count()} أجهزة من Active Directory.");
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشلت مزامنة أجهزة Active Directory: {ex.Message}");
        }
        finally
        {
            IsSyncing = false;
        }
    }

    [RelayCommand]
    private async Task SyncAdUsersAsync()
    {
        IsSyncing = true;
        try
        {
            var users = await _adService.SyncUsersAsync();
            AdUserCount = users.Count();
            LastUserSync = _adService.GetLastUserSyncDate();
            DialogHelper.ShowInfo($"تم مزامنة {users.Count()} مستخدمين من Active Directory.");
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشلت مزامنة مستخدمين Active Directory: {ex.Message}");
        }
        finally
        {
            IsSyncing = false;
        }
    }
}

public class SettingsGroup
{
    public string GroupName { get; set; } = string.Empty;
    public ObservableCollection<SettingItem> Settings { get; set; } = new();
}

public class SettingItem
{
    public string Key { get; set; } = string.Empty;
    public string DisplayKey { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
