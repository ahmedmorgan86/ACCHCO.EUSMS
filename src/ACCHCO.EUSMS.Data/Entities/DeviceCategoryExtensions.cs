namespace ACCHCO.EUSMS.Data.Entities;

public static class DeviceCategoryExtensions
{
    public static string ToArabicString(this DeviceCategory cat) => cat switch
    {
        DeviceCategory.All => "الكل",
        DeviceCategory.Computer => "أجهزة كمبيوتر",
        DeviceCategory.Printer => "طابعات",
        DeviceCategory.AccessPoint => "نقاط وصول",
        DeviceCategory.Switch => "موزعات شبكية",
        DeviceCategory.Camera => "كاميرات مراقبة",
        DeviceCategory.Phone => "هواتف شبكي",
        DeviceCategory.Server => "خوادم",
        DeviceCategory.RDT => "أجهزة لاسلكية (RDT)",
        DeviceCategory.Other => "أخرى",
        _ => cat.ToString()
    };
}
