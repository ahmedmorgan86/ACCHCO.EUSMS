using ClosedXML.Excel;
using ACCHCO.EUSMS.Data.Entities;
using System.Reflection;

namespace ACCHCO.EUSMS.Reports.Excel;

public class ExcelReportGenerator
{
    private static byte[]? LoadLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("ACCHCO.EUSMS.Reports.logo.png");
        if (stream == null) return null;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    // ---------- Shared styling helpers ----------

    private static void AddSheetHeader(IXLWorksheet ws, string title, string subtitle, int columns)
    {
        var logoBytes = LoadLogo();
        if (logoBytes != null)
        {
            using var logoStream = new MemoryStream(logoBytes);
            var pic = ws.AddPicture(logoStream);
            pic.MoveTo(ws.Cell(1, 1), 5, 0);
            pic.Scale(0.15);
        }

        ws.Cell(1, 3).Value = "شركة الأسكندرية لتداول الحاويات والبضائع";
        ws.Range(1, 1, 1, columns).Merge();
        ws.Range(1, 1, 1, columns).Style.Font.Bold = true;
        ws.Range(1, 1, 1, columns).Style.Font.FontSize = 16;
        ws.Range(1, 1, 1, columns).Style.Font.FontColor = XLColor.FromHtml("#2563EB");
        ws.Range(1, 1, 1, columns).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 34;

        ws.Cell(2, 1).Value = title;
        ws.Range(2, 1, 2, columns).Merge();
        ws.Range(2, 1, 2, columns).Style.Font.Bold = true;
        ws.Range(2, 1, 2, columns).Style.Font.FontSize = 12;
        ws.Range(2, 1, 2, columns).Style.Font.FontColor = XLColor.FromHtml("#0F172A");
        ws.Range(2, 1, 2, columns).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(3, 1).Value = subtitle +
            $"    |    تاريخ التصدير: {DateTime.Now:dd/MM/yyyy HH:mm}    |    أُنشئ بواسطة: {Environment.UserName}";
        ws.Range(3, 1, 3, columns).Merge();
        ws.Range(3, 1, 3, columns).Style.Font.FontSize = 9;
        ws.Range(3, 1, 3, columns).Style.Font.Italic = true;
        ws.Range(3, 1, 3, columns).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        ws.Range(3, 1, 3, columns).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(4, 1, 4, columns).Merge();
        ws.Range(4, 1, 4, columns).Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
        ws.Row(4).Height = 5;
    }

    private static void AddStatStrip(IXLWorksheet ws, int valueRow, int labelRow, (string Label, int Value, string Color)[] stats)
    {
        for (int i = 0; i < stats.Length; i++)
        {
            var v = ws.Cell(valueRow, i + 1);
            v.Value = stats[i].Value.ToString("N0");
            v.Style.Font.Bold = true;
            v.Style.Font.FontSize = 16;
            v.Style.Font.FontColor = XLColor.FromHtml(stats[i].Color);
            v.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            v.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var l = ws.Cell(labelRow, i + 1);
            l.Value = stats[i].Label;
            l.Style.Font.FontSize = 9;
            l.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            l.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.Row(valueRow).Height = 30;
    }

    private static void StyleDataTable(IXLWorksheet ws, int headerRow, int firstDataRow, int lastDataRow, int lastCol)
    {
        var headerRange = ws.Range(headerRow, 1, headerRow, lastCol);
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(headerRow).Height = 26;

        if (lastDataRow >= firstDataRow)
        {
            var dataRange = ws.Range(firstDataRow, 1, lastDataRow, lastCol);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
            dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            for (int r = firstDataRow; r <= lastDataRow; r++)
            {
                if ((r - firstDataRow) % 2 == 0)
                    ws.Range(r, 1, r, lastCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            }
        }

        ws.SheetView.FreezeRows(headerRow);
    }

    private static void SetupPrint(IXLWorksheet ws)
    {
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.Margins.Left = 0.3;
        ws.PageSetup.Margins.Right = 0.3;
        ws.PageSetup.Margins.Top = 0.4;
        ws.PageSetup.Margins.Bottom = 0.4;
    }

    private static void SetStatusCell(IXLCell cell, TicketStatus status)
    {
        var (color, light) = status switch
        {
            TicketStatus.Open => ("#2563EB", "#DBEAFE"),
            TicketStatus.InProgress => ("#D97706", "#FEF3C7"),
            TicketStatus.OnHold => ("#DC2626", "#FEE2E2"),
            TicketStatus.Resolved => ("#059669", "#D1FAE5"),
            _ => ("#64748B", "#F1F5F9")
        };
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(light);
        cell.Style.Font.FontColor = XLColor.FromHtml(color);
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void SetPriorityCell(IXLCell cell, Priority priority)
    {
        var (color, light) = priority switch
        {
            Priority.Low => ("#059669", "#D1FAE5"),
            Priority.Medium => ("#D97706", "#FEF3C7"),
            Priority.High => ("#EA580C", "#FFEDD5"),
            _ => ("#DC2626", "#FEE2E2")
        };
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(light);
        cell.Style.Font.FontColor = XLColor.FromHtml(color);
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void SetShiftCell(IXLCell cell, Shift shift)
    {
        var (color, light) = shift switch
        {
            Shift.Red => ("#DC2626", "#FEE2E2"),
            Shift.Yellow => ("#D97706", "#FEF3C7"),
            _ => ("#2563EB", "#DBEAFE")
        };
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(light);
        cell.Style.Font.FontColor = XLColor.FromHtml(color);
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void SetEquipmentStatusCell(IXLCell cell, EquipmentStatus status)
    {
        var (color, light) = status switch
        {
            EquipmentStatus.Active => ("#059669", "#D1FAE5"),
            EquipmentStatus.UnderMaintenance => ("#D97706", "#FEF3C7"),
            EquipmentStatus.Lost => ("#DC2626", "#FEE2E2"),
            _ => ("#64748B", "#F1F5F9")
        };
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(light);
        cell.Style.Font.FontColor = XLColor.FromHtml(color);
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    // ---------- Ticket report ----------

    public byte[] GenerateTicketReport(IEnumerable<Ticket> tickets, string title)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("تقرير التذاكر");
        ws.RightToLeft = true;

        var list = tickets?.ToList() ?? new List<Ticket>();
        const int cols = 20;

        AddSheetHeader(ws, title, "إدارة دعم المستخدمين للأجهزة - تقرير شامل بالتذاكر", cols);

        var total = list.Count;
        var open = list.Count(t => t.Status == TicketStatus.Open);
        var inProgress = list.Count(t => t.Status == TicketStatus.InProgress);
        var resolved = list.Count(t => t.Status is TicketStatus.Resolved or TicketStatus.Closed);
        var closed = list.Count(t => t.Status == TicketStatus.Closed);

        AddStatStrip(ws, 5, 6, new[]
        {
            ("إجمالي التذاكر", total, "#2563EB"),
            ("مفتوحة", open, "#D97706"),
            ("قيد التنفيذ", inProgress, "#0EA5E9"),
            ("تم الحل", resolved, "#059669"),
            ("مغلقة", closed, "#64748B")
        });

        const int headerRow = 8;
        var headers = new[]
        {
            "رقم التذكرة", "التاريخ", "الوقت", "الوردية", "أخصائي الدعم", "مقدم الطلب",
            "القسم", "نوع المعدة", "اسم المعدة", "رقم الأصل",
            "اسم الحاسوب", "عنوان IP", "نظام التشغيل", "نوع العطل",
            "الأولوية", "الحالة", "وصف المشكلة", "الإجراءات المتخذة",
            "الحل", "وقت الحل (دقيقة)"
        };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(headerRow, i + 1).Value = headers[i];

        var row = headerRow + 1;
        foreach (var t in list)
        {
            ws.Cell(row, 1).Value = t.TicketNumber ?? "";
            ws.Cell(row, 2).Value = t.TicketDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = t.TicketTime.ToString(@"hh\:mm");
            ws.Cell(row, 4).Value = t.Shift.ToArabicString();
            ws.Cell(row, 5).Value = t.SupportSpecialist?.FullName ?? "غير محدد";
            ws.Cell(row, 6).Value = t.RequesterName;
            ws.Cell(row, 7).Value = t.RequesterSection;
            ws.Cell(row, 8).Value = t.EquipmentType.ToArabicString();
            ws.Cell(row, 9).Value = t.EquipmentName ?? "";
            ws.Cell(row, 10).Value = t.AssetTag ?? "";
            ws.Cell(row, 11).Value = t.ComputerName ?? "";
            ws.Cell(row, 12).Value = t.IpAddress ?? "";
            ws.Cell(row, 13).Value = t.OperatingSystem ?? "";
            ws.Cell(row, 14).Value = t.FaultType.ToArabicString();
            ws.Cell(row, 15).Value = t.Priority.ToArabicString();
            ws.Cell(row, 16).Value = t.Status.ToArabicString();
            ws.Cell(row, 17).Value = t.ProblemDescription ?? "";
            ws.Cell(row, 18).Value = t.ActionsTaken ?? "";
            ws.Cell(row, 19).Value = t.Solution ?? "";
            ws.Cell(row, 20).Value = t.ResolutionTimeMinutes?.ToString("F0") ?? "0";
            row++;
        }

        var lastRow = row - 1;
        ws.Columns().AdjustToContents();
        StyleDataTable(ws, headerRow, headerRow + 1, lastRow, cols);

        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            SetShiftCell(ws.Cell(r, 4), list[r - headerRow - 1].Shift);
            SetPriorityCell(ws.Cell(r, 15), list[r - headerRow - 1].Priority);
            SetStatusCell(ws.Cell(r, 16), list[r - headerRow - 1].Status);
        }

        if (lastRow >= headerRow)
            ws.AutoFilter.IsEnabled = true;
            ws.AutoFilter.Range = ws.Range(headerRow, 1, lastRow, cols);
        SetupPrint(ws);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ---------- Equipment report ----------

    public byte[] GenerateEquipmentReport(IEnumerable<Equipment> equipment)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("تقرير المعدات والسيارات");
        ws.RightToLeft = true;

        var list = equipment?.ToList() ?? new List<Equipment>();
        const int cols = 10;

        AddSheetHeader(ws, "تقرير حصر المعدات والأجهزة", "شركة الأسكندرية لتداول الحاويات والبضائع - سجل المعدات الكامل", cols);

        var total = list.Count;
        var active = list.Count(e => e.Status == EquipmentStatus.Active);
        var maintenance = list.Count(e => e.Status == EquipmentStatus.UnderMaintenance);
        var inactive = list.Count(e => e.Status == EquipmentStatus.Inactive || e.Status == EquipmentStatus.Lost);

        AddStatStrip(ws, 5, 6, new[]
        {
            ("إجمالي المعدات", total, "#2563EB"),
            ("نشطة", active, "#059669"),
            ("تحت الصيانة", maintenance, "#D97706"),
            ("متوقفة", inactive, "#64748B")
        });

        const int headerRow = 8;
        var headers = new[]
        {
            "اسم المعدة/الجهاز", "النوع", "رقم الأصل", "الرقم المسلسل", "اسم الحاسوب",
            "عنوان IP", "الموقع", "القسم", "الحالة", "المستخدم/المسند إليه"
        };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(headerRow, i + 1).Value = headers[i];

        var row = headerRow + 1;
        foreach (var e in list)
        {
            ws.Cell(row, 1).Value = e.Name;
            ws.Cell(row, 2).Value = e.Type.ToArabicString();
            ws.Cell(row, 3).Value = e.AssetTag ?? "";
            ws.Cell(row, 4).Value = e.SerialNumber ?? "";
            ws.Cell(row, 5).Value = e.ComputerName ?? "";
            ws.Cell(row, 6).Value = e.IpAddress ?? "";
            ws.Cell(row, 7).Value = e.Location ?? "";
            ws.Cell(row, 8).Value = e.Section ?? "";
            ws.Cell(row, 9).Value = e.Status.ToArabicString();
            ws.Cell(row, 10).Value = e.AssignedTo ?? "";
            row++;
        }

        var lastRow = row - 1;
        ws.Columns().AdjustToContents();
        StyleDataTable(ws, headerRow, headerRow + 1, lastRow, cols);

        for (int r = headerRow + 1; r <= lastRow; r++)
            SetEquipmentStatusCell(ws.Cell(r, 9), list[r - headerRow - 1].Status);

        if (lastRow >= headerRow)
            ws.AutoFilter.IsEnabled = true;
            ws.AutoFilter.Range = ws.Range(headerRow, 1, lastRow, cols);
        SetupPrint(ws);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ---------- Audit report ----------

    public byte[] GenerateAuditReport(IEnumerable<ACCHCO.EUSMS.Data.Entities.AuditLog> logs)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("سجل المراجعة");
        ws.RightToLeft = true;

        var list = logs?.ToList() ?? new List<ACCHCO.EUSMS.Data.Entities.AuditLog>();
        const int cols = 7;

        AddSheetHeader(ws, "تقرير سجل المراجعة والتغييرات", "شركة الأسكندرية لتداول الحاويات والبضائع - Audit Log", cols);

        var total = list.Count;
        var users = list.Select(l => l.Username).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().Count();

        AddStatStrip(ws, 5, 6, new[]
        {
            ("إجمالي السجلات", total, "#2563EB"),
            ("مستخدمون فريدون", users, "#0EA5E9")
        });

        const int headerRow = 8;
        var headers = new[] { "التاريخ والوقت", "المستخدم", "الإجراء", "الكائن", "رقم الكائن", "التفاصيل", "اسم الجهاز" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(headerRow, i + 1).Value = headers[i];

        var row = headerRow + 1;
        foreach (var log in list)
        {
            ws.Cell(row, 1).Value = log.Timestamp.ToString("dd/MM/yyyy HH:mm:ss");
            ws.Cell(row, 2).Value = log.Username ?? "";
            ws.Cell(row, 3).Value = log.Action;
            ws.Cell(row, 4).Value = log.EntityName ?? "";
            ws.Cell(row, 5).Value = log.EntityId ?? "";
            ws.Cell(row, 6).Value = log.NewValues ?? log.Details ?? "";
            ws.Cell(row, 7).Value = log.MachineName ?? "";
            row++;
        }

        var lastRow = row - 1;
        ws.Columns().AdjustToContents();
        StyleDataTable(ws, headerRow, headerRow + 1, lastRow, cols);

        if (lastRow >= headerRow)
            ws.AutoFilter.IsEnabled = true;
            ws.AutoFilter.Range = ws.Range(headerRow, 1, lastRow, cols);
        SetupPrint(ws);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ---------- Performance section report ----------

    public byte[] GeneratePerformanceSectionReport(
        string title,
        IEnumerable<KeyValuePair<string, string>> kpis,
        IEnumerable<KeyValuePair<string, string>> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("تقرير الأداء");
        ws.RightToLeft = true;

        var kpiList = kpis?.ToList() ?? new List<KeyValuePair<string, string>>();
        var rowList = rows?.ToList() ?? new List<KeyValuePair<string, string>>();

        AddSheetHeader(ws, title, "تحليلات الأداء - إدارة دعم المستخدمين للأجهزة", 6);

        var row = 5;
        for (int i = 0; i < kpiList.Count; i++)
        {
            var labelCol = i % 2 == 0 ? 1 : 3;
            var label = ws.Cell(row, labelCol);
            label.Value = kpiList[i].Key + " :";
            label.Style.Font.Bold = true;
            label.Style.Font.FontColor = XLColor.FromHtml("#475569");
            label.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            label.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            label.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");

            var value = ws.Cell(row, labelCol + 1);
            value.Value = kpiList[i].Value;
            value.Style.Font.Bold = true;
            value.Style.Font.FontColor = XLColor.FromHtml("#2563EB");
            value.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            value.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");

            if (i % 2 == 1) row++;
        }
        if (kpiList.Count % 2 == 1) row++;
        row++;

        ws.Cell(row, 1).Value = "البند";
        ws.Cell(row, 2).Value = "العدد";
        var headerRow = row;
        row++;

        foreach (var item in rowList)
        {
            ws.Cell(row, 1).Value = item.Key;
            ws.Cell(row, 2).Value = item.Value;
            row++;
        }

        var lastRow = row - 1;
        StyleDataTable(ws, headerRow, headerRow + 1, lastRow, 2);
        ws.Columns().AdjustToContents();

        if (lastRow >= headerRow)
            ws.AutoFilter.IsEnabled = true;
            ws.AutoFilter.Range = ws.Range(headerRow, 1, lastRow, 2);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}

