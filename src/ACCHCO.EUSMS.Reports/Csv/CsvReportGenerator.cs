using System.Text;
using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Reports.Csv;

public class CsvReportGenerator
{
    public string GenerateTicketReport(IEnumerable<Ticket> tickets)
    {
        var sb = new StringBuilder();
        sb.AppendLine("رقم التذكرة,التاريخ,الوقت,الوردية,أخصائي الدعم,مقدم الطلب,القسم," +
            "نوع المعدة,اسم المعدة,رقم الأصل,اسم الكمبيوتر,عنوان IP,نظام التشغيل," +
            "نوع العطل,الأولوية,الحالة,وصف المشكلة,الإجراءات المتخذة,الحل," +
            "وقت الحل (دقيقة)");

        foreach (var t in tickets)
        {
            sb.AppendLine(string.Join(",",
                Escape(t.TicketNumber),
                t.TicketDate.ToString("dd/MM/yyyy"),
                t.TicketTime.ToString(@"hh\:mm"),
                t.Shift.ToArabicString(),
                Escape(t.SupportSpecialist?.FullName ?? "غير محدد"),
                Escape(t.RequesterName),
                Escape(t.RequesterSection),
                t.EquipmentType.ToArabicString(),
                Escape(t.EquipmentName ?? ""),
                Escape(t.AssetTag ?? ""),
                Escape(t.ComputerName ?? ""),
                Escape(t.IpAddress ?? ""),
                Escape(t.OperatingSystem ?? ""),
                t.FaultType.ToArabicString(),
                t.Priority.ToArabicString(),
                t.Status.ToArabicString(),
                Escape(t.ProblemDescription),
                Escape(t.ActionsTaken ?? ""),
                Escape(t.Solution ?? ""),
                t.ResolutionTimeMinutes?.ToString("F0") ?? "0"
            ));
        }

        return sb.ToString();
    }

    public string GenerateEquipmentReport(IEnumerable<Equipment> equipment)
    {
        var sb = new StringBuilder();
        sb.AppendLine("اسم المعدة/الجهاز,النوع,رقم الأصل,الرقم المسلسل,اسم الكمبيوتر,عنوان IP," +
            "الموقع,القسم,الحالة,المستخدم/المسند إليه");

        foreach (var e in equipment)
        {
            sb.AppendLine(string.Join(",",
                Escape(e.Name),
                e.Type.ToArabicString(),
                Escape(e.AssetTag ?? ""),
                Escape(e.SerialNumber ?? ""),
                Escape(e.ComputerName ?? ""),
                Escape(e.IpAddress ?? ""),
                Escape(e.Location ?? ""),
                Escape(e.Section ?? ""),
                e.Status.ToArabicString(),
                Escape(e.AssignedTo ?? "")
            ));
        }

        return sb.ToString();
    }

    public byte[] ToBytes(string csvContent)
    {
        var bom = Encoding.UTF8.GetPreamble();
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        return bom.Concat(bytes).ToArray();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
