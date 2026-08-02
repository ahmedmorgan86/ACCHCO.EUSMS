using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ACCHCO.EUSMS.Data.Entities;
using System.Reflection;

namespace ACCHCO.EUSMS.Reports.Pdf;

public class PdfReportGenerator
{
    private const string BrandBlue = "#2563EB";
    private const string BrandDark = "#1D4ED8";
    private const string HeaderDark = "#1E293B";
    private const string Ink = "#0F172A";
    private const string BodyText = "#334155";
    private const string Muted = "#64748B";
    private const string Faint = "#94A3B8";
    private const string BorderLight = "#E2E8F0";
    private const string RowAlt = "#F8FAFC";

    private static byte[] LoadLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("ACCHCO.EUSMS.Reports.logo.png");
        if (stream == null) return Array.Empty<byte>();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    // ---------- Shared layout helpers ----------

    private static void AddReportHeader(IContainer c, byte[] logoBytes, string title, DateTime generatedDate)
    {
        c.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    if (logoBytes.Length > 0)
                        inner.Item().Width(56).Image(logoBytes);
                    inner.Item().PaddingTop(2).Text("شركة الأسكندرية لتداول الحاويات والبضائع")
                        .FontSize(12).Bold().FontColor(Ink);
                    inner.Item().Text("ACCHCO - End User Support Department")
                        .FontSize(8).FontColor(Muted);
                });

                row.RelativeItem().AlignLeft().Column(inner =>
                {
                    inner.Item().Text(title).FontSize(13).Bold().FontColor(BrandBlue);
                    inner.Item().Text($"تاريخ الإصدار: {generatedDate:dd/MM/yyyy HH:mm}  |  أُنشئ بواسطة: {Environment.UserName}")
                        .FontSize(8).FontColor(Muted);
                });
            });
            col.Item().PaddingTop(6).LineHorizontal(2f).LineColor(BrandBlue);
            col.Item().PaddingTop(2).LineHorizontal(0.7f).LineColor(BorderLight);
        });
    }

    private static void AddReportFooter(IContainer c, string title)
    {
        c.AlignCenter().Text(text =>
        {
            text.Span("نظام إدارة دعم المستخدمين للأجهزة EUSMS | ").FontSize(7).FontColor(Faint);
            text.CurrentPageNumber().FontSize(7).FontColor(Faint);
            text.Span($" | {title}").FontSize(7).FontColor(Faint);
        });
    }

    private static void AddStatCard(RowDescriptor row, string value, string label, string color)
    {
        row.RelativeItem().Padding(3).Border(1f).BorderColor(BorderLight)
            .Background(RowAlt).Padding(8).Column(inner =>
            {
                inner.Item().Text(value).FontSize(15).Bold().FontColor(color);
                inner.Item().Text(label).FontSize(8).FontColor(Muted);
            });
    }

    private static void WriteSectionHeader(ColumnDescriptor col, string title)
    {
        col.Item().PaddingTop(12).PaddingBottom(6).Row(row =>
        {
            row.AutoItem().Width(4).Height(13).Background(BrandBlue);
            row.RelativeItem().PaddingRight(6).Text(title).FontSize(11).Bold().FontColor(Ink);
        });
    }

    private static void WriteInfoRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(150).Text($"{label}:").FontSize(9).Bold().FontColor(Muted);
            row.RelativeItem().Text(value).FontSize(9).FontColor(Ink);
        });
    }

    private static void WritePillRow(ColumnDescriptor col, string label, string value, string color)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(150).Text($"{label}:").FontSize(9).Bold().FontColor(Muted);
            row.RelativeItem().AlignMiddle().Background(color).PaddingHorizontal(8).PaddingVertical(2)
                .Text(value).FontSize(9).FontColor(Colors.White).Bold();
        });
    }

    private static void WritePillCell(IContainer cell, string text, string color)
    {
        cell.BorderBottom(0.5f).BorderColor(BorderLight).Padding(3).AlignCenter()
            .Background(color).PaddingHorizontal(6).PaddingVertical(2)
            .Text(text).FontSize(7.5f).FontColor(Colors.White).Bold();
    }

    private static string StatusColor(TicketStatus s) => s switch
    {
        TicketStatus.Open => "#3B82F6",
        TicketStatus.InProgress => "#F59E0B",
        TicketStatus.OnHold => "#EF4444",
        TicketStatus.Resolved => "#10B981",
        _ => "#64748B"
    };

    private static string PriorityColor(Priority p) => p switch
    {
        Priority.Low => "#10B981",
        Priority.Medium => "#F59E0B",
        Priority.High => "#F97316",
        _ => "#EF4444"
    };

    private static string ShiftColor(Shift s) => s switch
    {
        Shift.Red => "#EF4444",
        Shift.Yellow => "#F59E0B",
        _ => "#3B82F6"
    };

    private static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "غير محدد";
        return text.Length > maxLength ? text[..maxLength] + "..." : text;
    }

    // ---------- Ticket list report ----------

    public byte[] GenerateTicketReport(IEnumerable<Ticket> tickets, string title, DateTime generatedDate)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var logoBytes = LoadLogo();
        var list = tickets?.ToList() ?? new List<Ticket>();

        var total = list.Count;
        var open = list.Count(t => t.Status == TicketStatus.Open);
        var inProgress = list.Count(t => t.Status == TicketStatus.InProgress);
        var resolved = list.Count(t => t.Status is TicketStatus.Resolved or TicketStatus.Closed);
        var closed = list.Count(t => t.Status == TicketStatus.Closed);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(15);
                page.MarginVertical(18);
                page.ContentFromRightToLeft();

                page.Header().Element(h => AddReportHeader(h, logoBytes, title, generatedDate));

                page.Content().PaddingVertical(8).Element(content =>
                {
                    content.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            AddStatCard(row, total.ToString("N0"), "إجمالي التذاكر", BrandBlue);
                            AddStatCard(row, open.ToString("N0"), "مفتوحة", "#F59E0B");
                            AddStatCard(row, inProgress.ToString("N0"), "قيد التنفيذ", "#0EA5E9");
                            AddStatCard(row, resolved.ToString("N0"), "تم الحل", "#10B981");
                            AddStatCard(row, closed.ToString("N0"), "مغلقة", "#64748B");
                        });
                        col.Item().PaddingTop(8);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(58);
                                columns.ConstantColumn(52);
                                columns.ConstantColumn(52);
                                columns.ConstantColumn(85);
                                columns.ConstantColumn(100);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(52);
                                columns.ConstantColumn(52);
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                var headers = new[] { "رقم التذكرة", "التاريخ", "الوردية", "مقدم الطلب",
                                    "المعدة/الجهاز", "نوع العطل", "الأولوية", "الحالة", "وصف المشكلة" };
                                foreach (var h in headers)
                                {
                                    header.Cell().Background(HeaderDark).Padding(4).AlignCenter()
                                        .Text(h).FontSize(8).FontColor(Colors.White).Bold();
                                }
                            });

                            if (list.Count == 0)
                            {
                                table.Cell().ColumnSpan(9).Background(RowAlt).Padding(12).AlignCenter()
                                    .Text("لا توجد بيانات لعرضها").FontSize(9).FontColor(Muted);
                            }

                            foreach (var t in list)
                            {
                                var eqStr = $"{t.EquipmentType.ToArabicString()} - {t.EquipmentName ?? ""}".TrimEnd(' ', '-');

                                table.Cell().BorderBottom(0.5f).BorderColor(BorderLight).Padding(3).AlignCenter()
                                    .Text(t.TicketNumber ?? "-").FontSize(7.5f).FontColor(Ink);
                                table.Cell().BorderBottom(0.5f).BorderColor(BorderLight).Padding(3).AlignCenter()
                                    .Text(t.TicketDate.ToString("dd/MM/yyyy")).FontSize(7.5f).FontColor(BodyText);
                                WritePillCell(table.Cell(), t.Shift.ToArabicString(), ShiftColor(t.Shift));
                                table.Cell().BorderBottom(0.5f).BorderColor(BorderLight).Padding(3)
                                    .Text(t.RequesterName).FontSize(7.5f).FontColor(Ink);
                                table.Cell().BorderBottom(0.5f).BorderColor(BorderLight).Padding(3)
                                    .Text(eqStr).FontSize(7.5f).FontColor(Ink);
                                table.Cell().BorderBottom(0.5f).BorderColor(BorderLight).Padding(3)
                                    .Text(t.FaultType.ToArabicString()).FontSize(7.5f).FontColor(BodyText);
                                WritePillCell(table.Cell(), t.Priority.ToArabicString(), PriorityColor(t.Priority));
                                WritePillCell(table.Cell(), t.Status.ToArabicString(), StatusColor(t.Status));
                                table.Cell().BorderBottom(0.5f).BorderColor(BorderLight).Padding(3)
                                    .Text(Truncate(t.ProblemDescription, 60)).FontSize(7f).FontColor(BodyText);
                            }
                        });
                    });
                });

                page.Footer().Element(f => AddReportFooter(f, title));
            });
        });

        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return ms.ToArray();
    }

    // ---------- Performance section report ----------

    public byte[] GeneratePerformanceSectionReport(
        string title,
        IEnumerable<KeyValuePair<string, string>> kpis,
        IEnumerable<KeyValuePair<string, string>> rows)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var logoBytes = LoadLogo();
        var kpiList = kpis?.ToList() ?? new List<KeyValuePair<string, string>>();
        var rowList = rows?.ToList() ?? new List<KeyValuePair<string, string>>();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.ContentFromRightToLeft();

                page.Header().Element(h => AddReportHeader(h, logoBytes, title, DateTime.Now));

                page.Content().PaddingVertical(10).Element(content =>
                {
                    content.Column(col =>
                    {
                        for (int start = 0; start < kpiList.Count; start += 4)
                        {
                            var chunk = kpiList.Skip(start).Take(4).ToList();
                            col.Item().Row(row =>
                            {
                                foreach (var kpi in chunk)
                                {
                                    row.RelativeItem().Padding(2).Border(1f).BorderColor(BorderLight)
                                        .Background(RowAlt).Padding(6).Column(inner =>
                                        {
                                            inner.Item().Text(kpi.Value).FontSize(13).Bold().FontColor(BrandBlue);
                                            inner.Item().Text(kpi.Key).FontSize(7.5f).FontColor(Muted);
                                        });
                                }
                            });
                        }
                        col.Item().PaddingBottom(10);

                        WriteSectionHeader(col, $"تفاصيل - {title}");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderDark).Padding(5).AlignCenter()
                                    .Text("البند").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderDark).Padding(5).AlignCenter()
                                    .Text("العدد").FontSize(9).FontColor(Colors.White).Bold();
                            });

                            if (rowList.Count == 0)
                            {
                                table.Cell().ColumnSpan(2).Background(RowAlt).Padding(12).AlignCenter()
                                    .Text("لا توجد بيانات لعرضها").FontSize(9).FontColor(Muted);
                            }

                            foreach (var item in rowList)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(BorderLight).Padding(4)
                                    .Text(item.Key).FontSize(8.5f).FontColor(Ink);
                                table.Cell().BorderBottom(0.5f).BorderColor(BorderLight).Padding(4).AlignCenter()
                                    .Text(item.Value).FontSize(8.5f).FontColor(BrandBlue).Bold();
                            }
                        });
                    });
                });

                page.Footer().Element(f => AddReportFooter(f, title));
            });
        });

        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return ms.ToArray();
    }

    // ---------- Single ticket report ----------

    public byte[] GenerateSingleTicketReport(Ticket ticket)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var logoBytes = LoadLogo();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.ContentFromRightToLeft();

                page.Header().Element(h =>
                {
                    h.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            if (logoBytes.Length > 0)
                                row.ConstantItem(64).Image(logoBytes);
                            row.RelativeItem().PaddingRight(8).Column(inner =>
                            {
                                inner.Item().Text("شركة الأسكندرية لتداول الحاويات والبضائع")
                                    .FontSize(13).Bold().FontColor(Ink);
                                inner.Item().Text("إدارة دعم المستخدمين للأجهزة - End User Support Department")
                                    .FontSize(8).FontColor(Muted);
                            });
                            row.RelativeItem().AlignLeft().Column(inner =>
                            {
                                inner.Item().AlignLeft().Background(BrandBlue)
                                    .PaddingHorizontal(12).PaddingVertical(5)
                                    .Text($"تذكرة رقم: {ticket.TicketNumber}").FontSize(11).FontColor(Colors.White).Bold();
                                inner.Item().PaddingTop(4).AlignLeft().Text(
                                        $"تاريخ الإصدار: {DateTime.Now:dd/MM/yyyy HH:mm}  |  أُنشئ بواسطة: {Environment.UserName}")
                                    .FontSize(8).FontColor(Muted);
                            });
                        });
                        col.Item().PaddingTop(6).LineHorizontal(2f).LineColor(BrandBlue);
                        col.Item().PaddingTop(2).LineHorizontal(0.7f).LineColor(BorderLight);
                    });
                });

                page.Content().PaddingVertical(12).Element(content =>
                {
                    content.Column(col =>
                    {
                        WriteSectionHeader(col, "بيانات التذكرة الأساسية");
                        WriteInfoRow(col, "رقم التذكرة", ticket.TicketNumber);
                        WriteInfoRow(col, "التاريخ", ticket.TicketDate.ToString("dd/MM/yyyy"));
                        WriteInfoRow(col, "الوقت", ticket.TicketTime.ToString(@"hh\:mm"));
                        WritePillRow(col, "الوردية", ticket.Shift.ToArabicString(), ShiftColor(ticket.Shift));
                        WritePillRow(col, "الحالة", ticket.Status.ToArabicString(), StatusColor(ticket.Status));
                        WritePillRow(col, "الأولوية", ticket.Priority.ToArabicString(), PriorityColor(ticket.Priority));

                        WriteSectionHeader(col, "بيانات مقدم الطلب");
                        WriteInfoRow(col, "الاسم", ticket.RequesterName);
                        WriteInfoRow(col, "القسم", ticket.RequesterSection);
                        WriteInfoRow(col, "البريد الإلكتروني", ticket.RequesterEmail ?? "غير محدد");
                        WriteInfoRow(col, "الهاتف / الداخلي", ticket.RequesterPhone ?? "غير محدد");

                        WriteSectionHeader(col, "بيانات المعدة والجهاز");
                        WriteInfoRow(col, "نوع المعدة", ticket.EquipmentType.ToArabicString());
                        WriteInfoRow(col, "اسم المعدة", ticket.EquipmentName ?? "غير محدد");
                        WriteInfoRow(col, "رقم الأصل (Asset Tag)", ticket.AssetTag ?? "غير محدد");
                        WriteInfoRow(col, "اسم الحاسوب", ticket.ComputerName ?? "غير محدد");
                        WriteInfoRow(col, "عنوان IP", ticket.IpAddress ?? "غير محدد");
                        WriteInfoRow(col, "نظام التشغيل", ticket.OperatingSystem ?? "غير محدد");

                        WriteSectionHeader(col, "تفاصيل العطل والحل");
                        WriteInfoRow(col, "نوع العطل", ticket.FaultType.ToArabicString());
                        WriteInfoRow(col, "الموقع", ticket.Location ?? "غير محدد");

                        col.Item().PaddingTop(5).Text("وصف المشكلة:").FontSize(9).Bold().FontColor(BodyText);
                        col.Item().PaddingBottom(6).Text(ticket.ProblemDescription).FontSize(9).FontColor(Ink);

                        if (!string.IsNullOrEmpty(ticket.ActionsTaken))
                        {
                            col.Item().PaddingTop(3).Text("الإجراءات المتخذة:").FontSize(9).Bold().FontColor(BodyText);
                            col.Item().PaddingBottom(6).Text(ticket.ActionsTaken).FontSize(9).FontColor(Ink);
                        }

                        if (!string.IsNullOrEmpty(ticket.Solution))
                        {
                            col.Item().PaddingTop(3).Text("الحل النهائي:").FontSize(9).Bold().FontColor(BodyText);
                            col.Item().PaddingBottom(6).Text(ticket.Solution).FontSize(9).FontColor(Ink);
                        }

                        if (!string.IsNullOrEmpty(ticket.Notes))
                        {
                            col.Item().PaddingTop(3).Text("ملاحظات:").FontSize(9).Bold().FontColor(BodyText);
                            col.Item().PaddingBottom(6).Text(ticket.Notes).FontSize(9).FontColor(Ink);
                        }

                        WriteSectionHeader(col, "معلومات الإغلاق والدعم");
                        WriteInfoRow(col, "وقت البداية", ticket.StartTime?.ToString("dd/MM/yyyy HH:mm") ?? "غير محدد");
                        WriteInfoRow(col, "وقت الانتهاء", ticket.FinishTime?.ToString("dd/MM/yyyy HH:mm") ?? "غير محدد");
                        WriteInfoRow(col, "مدة الحل",
                            ticket.ResolutionTimeMinutes.HasValue
                                ? $"{ticket.ResolutionTimeMinutes.Value:F0} دقيقة"
                                : "غير محدد");
                        WriteInfoRow(col, "أخصائي الدعم التقني",
                            ticket.SupportSpecialist?.FullName ?? "غير محدد");

                        col.Item().PaddingTop(20).LineHorizontal(1f).LineColor(BorderLight);
                        col.Item().PaddingTop(8).Text(
                                "توقيع أخصائي الدعم: ______________________        توقيع مقدم الطلب: ______________________")
                            .FontSize(9).FontColor(Muted);
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("ACCHCO EUSMS | ").FontSize(7).FontColor(Faint);
                    text.CurrentPageNumber().FontSize(7).FontColor(Faint);
                    text.Span($" | تذكرة رقم {ticket.TicketNumber}").FontSize(7).FontColor(Faint);
                });
            });
        });

        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return ms.ToArray();
    }

    // ---------- User manual ----------

    public byte[] GenerateUserManualPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var logoBytes = LoadLogo();

        var coverPage = new Func<IDocumentContainer, IDocumentContainer>(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    if (logoBytes.Length > 0)
                        col.Item().AlignCenter().Width(110).Image(logoBytes);
                    col.Item().PaddingTop(24).AlignCenter().Text("شركة الأسكندرية لتداول الحاويات والبضائع")
                        .FontSize(16).Bold().FontColor(Ink);
                    col.Item().AlignCenter().Text("ACCHCO - Alexandria Container & Cargo Handling Company")
                        .FontSize(10).FontColor(Muted);
                    col.Item().PaddingTop(28).AlignCenter().Width(150).Height(5).Background(BrandBlue);
                    col.Item().PaddingTop(20).AlignCenter().Text("دليل استخدام النظام").FontSize(30).Bold().FontColor(BrandBlue);
                    col.Item().AlignCenter().Text("نظام إدارة دعم المستخدمين للأجهزة").FontSize(16).FontColor(BodyText);
                    col.Item().PaddingTop(6).AlignCenter().Text("End User Support Management System (EUSMS)")
                        .FontSize(11).FontColor(Muted);

                    col.Item().PaddingTop(44).AlignCenter().Border(1f).BorderColor(BorderLight)
                        .Background(RowAlt).Padding(16).Column(info =>
                        {
                            info.Item().AlignCenter().Text($"الإصدار: 1.0    |    تاريخ الإصدار: {DateTime.Now:yyyy-MM-dd}")
                                .FontSize(10).FontColor(BodyText);
                            info.Item().PaddingTop(6).AlignCenter().Text("© Ahmed Morgan 2026").FontSize(10).FontColor(Muted);
                            info.Item().PaddingTop(6).AlignCenter().Text($"أُنشئ بواسطة: {Environment.UserName}").FontSize(9).FontColor(Muted);
                        });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("ACCHCO EUSMS").FontSize(8).Bold().FontColor(BrandBlue);
                    text.Span("   |   دليل الاستخدام الرسمي").FontSize(8).FontColor(Muted);
                });
            });
            return container;
        });

        var document = Document.Create(container =>
        {
            coverPage(container);

            AddContentPage(container, "نظرة عامة", logoBytes, new[]
            {
                "نظام إدارة دعم المستخدمين للأجهزة (EUSMS) هو نظام متكامل لإدارة تذاكر الدعم التقني، الأجهزة والمعدات، استبدال الأجهزة، وتوثيق تسليم الورديات.",
                "شركة الأسكندرية لتداول الحاويات والبضائع (ACCHCO)"
            });

            AddContentPage(container, "لوحة التحكم (Dashboard)", logoBytes, new[]
            {
                "تعرض مؤشرات الأداء الرئيسية فور الدخول للتطبيق.",
                "عدد تذاكر اليوم - التذاكر المفتوحة - المغلقة.",
                "متوسط وقت الحل بالدقائق.",
                "إجمالي المعدات المسجلة وعدد المستخدمين.",
                "توزيع التذاكر حسب الورديات (الحمراء - الصفراء - الزرقاء).",
                "آخر التذاكر المضافة وحالتها."
            });

            AddContentPage(container, "إدارة التذاكر (Tickets)", logoBytes, new[]
            {
                "إضافة تذكرة جديدة: الضغط على زر 'إضافة تذكرة' يفتح نافذة منبثقة مبسطة.",
                "يتم تعبئة بيانات مقدم الطلب تلقائياً من الدليل النشط (Active Directory) أو بيانات المستخدم.",
                "اختيار الوردية (الحمراء، الصفراء، الزرقاء)، الأولوية، ونوع العطل.",
                "اختيار المعدة أو إدخال اسمها ووصف المشكلة.",
                "عرض التفاصيل الكاملة وتحديث الحالة (مفتوحة، قيد التنفيذ، معلقة، تم الحل، مغلقة).",
                "حل التذكرة وإغلاقها مع تسجيل الإجراءات والحل النهائي.",
                "طباعة التذكرة بصيغة PDF."
            });

            AddContentPage(container, "المعدات والأجهزة (Equipment)", logoBytes, new[]
            {
                "قاعدة بيانات مبسطة وفعالة لإدارة الأجهزة.",
                "تحتوي على 82 جهازاً أساسياً جاهزاً (AP، RTG، Kalmar، RS، Hyster، RDT).",
                "إضافة أو تعديل معدة: الاسم، النوع، رقم الأصل، الرقم التسلسلي، عنوان IP، الحالة، وملاحظات.",
                "تصفية المعدات حسب النوع والحالة."
            });

            AddContentPage(container, "أجهزة الشبكة والمسح (Network Devices)", logoBytes, new[]
            {
                "مسح واكتشاف جميع الأجهزة على شبكات الشركة عبر 4 شبكات فرعية: 172.17.10, 172.17.30, 172.17.20, 172.17.70.",
                "اكتشاف الأجهزة بجميع أنواعها (حواسيب، طابعات، كاميرات، موزعات، نقاط وصول) عبر دمج فحص Ping وجداول ARP.",
                "إمكانية ربط أجهزة الشبكة بالمعدات أو إنشاء تذكرة دعم مباشرة لأي جهاز مشتغل بضغطة زر."
            });

            AddContentPage(container, "استبدال الأجهزة (Device Replacements)", logoBytes, new[]
            {
                "توثيق عمليات استبدال الأجهزة والتالف منها.",
                "الضغط على 'تسجيل استبدال جديد' يفتح نافذة منبثقة.",
                "اختيار الجهاز القديم والجهاز الجديد، تحديد التاريخ، وإدخال سبب الاستبدال.",
                "عرض سجل كامل لعمليات الاستبدال السابقة."
            });

            AddContentPage(container, "تسليم الورديات (Shift Handover)", logoBytes, new[]
            {
                "توثيق عمليات تسليم الورديات بين فرق الدعم (الوردية الحمراء، الصفراء، الزرقاء).",
                "تسجيل تسليمة وردية جديدة (التاريخ، من وردية، إلى وردية).",
                "إدخال الملخص، المهام المعلقة، والملاحظات الهامة."
            });

            AddContentPage(container, "الإعدادات والمزامنة (Settings)", logoBytes, new[]
            {
                "إعدادات النظام والربط مع الدليل النشط (Active Directory).",
                "التحقق من حالة الاتصال بـ Active Directory.",
                "مزامنة الأجهزة والمستخدمين بضغطة زر واحدة.",
                "تعديل إعدادات النظام باللغة العربية وحفظها في قاعدة البيانات."
            });

            AddContentPage(container, "نصائح سريعة", logoBytes, new[]
            {
                "جميع النوافذ المنبثقة تتكيف تلقائياً مع حجم المحتوى لضمان ظهور كافة الحقول بوضوح.",
                "استخدم القائمة الجانبية للتنقل السريع بين أقسام النظام المختلفة.",
                "يمكنك تحميل هذا الدليل كملف PDF في أي وقت بالضغط على زر التحميل في صفحة دليل الاستخدام."
            });
        });

        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return ms.ToArray();
    }

    private static void AddContentPage(IDocumentContainer container, string title, byte[] logoBytes, string[] items)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);
            page.ContentFromRightToLeft();

            page.Header().Element(h =>
            {
                h.Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (logoBytes.Length > 0)
                            row.ConstantItem(38).Image(logoBytes);
                        row.RelativeItem().PaddingRight(6).Text("دليل استخدام EUSMS").FontSize(9).Bold().FontColor(BrandBlue);
                        row.RelativeItem().AlignLeft().Text(title).FontSize(9).Bold().FontColor(Ink);
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(BrandBlue);
                });
            });

            page.Content().PaddingVertical(10).Element(content =>
            {
                content.Column(col =>
                {
                    col.Item().PaddingBottom(8).Row(row =>
                    {
                        row.AutoItem().Width(4).Height(15).Background(BrandBlue);
                        row.RelativeItem().PaddingRight(6).Text(title).FontSize(14).Bold().FontColor(Ink);
                    });

                    foreach (var item in items)
                    {
                        col.Item().PaddingBottom(5).Row(row =>
                        {
                            row.AutoItem().Width(4).Height(10).Background("#BFDBFE").AlignTop();
                            row.RelativeItem().PaddingRight(8).Text(item).FontSize(9).FontColor(BodyText);
                        });
                    }
                });
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.CurrentPageNumber().FontSize(7);
                text.Span(" | ").FontSize(7);
                text.Span("ACCHCO EUSMS - دليل الاستخدام").FontSize(7).FontColor(Muted);
            });
        });
    }
}

