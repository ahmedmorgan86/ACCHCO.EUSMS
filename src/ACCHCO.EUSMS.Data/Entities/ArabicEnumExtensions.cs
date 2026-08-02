namespace ACCHCO.EUSMS.Data.Entities;

public static class ArabicEnumExtensions
{
    public static string ToArabicString(this EquipmentType type) => type switch
    {
        EquipmentType.RTG => "ونش ساحة (RTG)",
        EquipmentType.Kalmar => "معدة كالمر (Calmr)",
        EquipmentType.Hyster => "معدة هيستر (Hastr)",
        EquipmentType.ReachStacker => "رافع حاويات (RS)",
        EquipmentType.AccessPoint => "نقطة وصول (Access Points)",
        EquipmentType.PC => "جهاز كمبيوتر (PC)",
        EquipmentType.Printer => "طابعة (Printers)",
        EquipmentType.GatesOcrCamera => "كاميرات البوابات (Gates OCR Cameras)",
        EquipmentType.RDT => "جهاز لاسلكي (RDT)",
        _ => type.ToString()
    };

    public static string ToArabicString(this TicketStatus status) => status switch
    {
        TicketStatus.Open => "مفتوحة",
        TicketStatus.InProgress => "قيد التنفيذ",
        TicketStatus.OnHold => "موقوفة مؤقتاً",
        TicketStatus.Resolved => "تم الحل",
        TicketStatus.Closed => "مغلقة",
        _ => status.ToString()
    };

    public static string ToArabicString(this Priority priority) => priority switch
    {
        Priority.Low => "منخفضة",
        Priority.Medium => "متوسطة",
        Priority.High => "عالية",
        Priority.Critical => "حرجة جداً",
        _ => priority.ToString()
    };

    public static string ToArabicString(this Shift shift) => shift switch
    {
        Shift.Red => "الحمراء",
        Shift.Yellow => "الصفراء",
        Shift.Blue => "الزرقاء",
        _ => shift.ToString()
    };

    public static string ToArabicString(this FaultType fault) => fault switch
    {
        FaultType.Hardware => "أعطال أجهزة (Hardware)",
        FaultType.Software => "أعطال برامج (Software)",
        FaultType.Network => "أعطال شبكة (Network)",
        FaultType.Connectivity => "مشاكل اتصال (Connectivity)",
        FaultType.Performance => "بطء وأداء (Performance)",
        FaultType.Security => "أمان وحماية (Security)",
        FaultType.Configuration => "إعدادات وتكفيات (Configuration)",
        FaultType.UserError => "خطأ مستخدم (User Error)",
        FaultType.Other => "أعطال أخرى (Other)",
        _ => fault.ToString()
    };

    public static string ToArabicString(this UserRole role) => role switch
    {
        UserRole.SystemAdministrator => "مدير النظام",
        UserRole.DepartmentManager => "مدير الإدارة",
        UserRole.SupportSpecialist => "أخصائي دعم تقني",
        UserRole.ReadOnly => "قراءة فقط",
        _ => role.ToString()
    };

    public static string ToArabicString(this EquipmentStatus status) => status switch
    {
        EquipmentStatus.Active => "نشطة",
        EquipmentStatus.Inactive => "غير نشطة",
        EquipmentStatus.UnderMaintenance => "تحت الصيانة",
        EquipmentStatus.Lost => "مفقودة",
        _ => status.ToString()
    };

    public static string ToArabicString(this IncidentSeverity s) => s switch
    {
        IncidentSeverity.Low => "منخفضة",
        IncidentSeverity.Medium => "متوسطة",
        IncidentSeverity.High => "عالية",
        IncidentSeverity.Critical => "حرجة",
        _ => s.ToString()
    };

    public static string ToArabicString(this IncidentImpact s) => s switch
    {
        IncidentImpact.SingleUser => "مستخدم واحد",
        IncidentImpact.Department => "إدارة واحدة",
        IncidentImpact.MultipleDepartments => "عدة إدارات",
        IncidentImpact.EntireOrganization => "المنظمة بالكامل",
        _ => s.ToString()
    };

    public static string ToArabicString(this IncidentUrgency s) => s switch
    {
        IncidentUrgency.Low => "منخفضة",
        IncidentUrgency.Medium => "متوسطة",
        IncidentUrgency.High => "عالية",
        _ => s.ToString()
    };

    public static string ToArabicString(this IncidentSource s) => s switch
    {
        IncidentSource.Phone => "هاتف",
        IncidentSource.Email => "بريد إلكتروني",
        IncidentSource.Portal => "بوابة",
        IncidentSource.WalkIn => "حضور مباشر",
        IncidentSource.Monitoring => "مراقبة",
        IncidentSource.Other => "أخرى",
        _ => s.ToString()
    };

    public static string ToArabicString(this WorkflowStatus s) => s switch
    {
        WorkflowStatus.New => "جديد",
        WorkflowStatus.Assigned => "تم التعيين",
        WorkflowStatus.Accepted => "مقبول",
        WorkflowStatus.InProgress => "قيد التنفيذ",
        WorkflowStatus.WaitingUser => "بانتظار المستخدم",
        WorkflowStatus.WaitingOther => "بانتظار قسم آخر",
        WorkflowStatus.Pending => "معلق",
        WorkflowStatus.Resolved => "تم الحل",
        WorkflowStatus.Closed => "مغلق",
        WorkflowStatus.Cancelled => "ملغي",
        _ => s.ToString()
    };

    public static string ToArabicString(this ChangeType s) => s switch
    {
        ChangeType.Standard => "قياسي",
        ChangeType.Normal => "عادي",
        ChangeType.Emergency => "طارئ",
        _ => s.ToString()
    };

    public static string ToArabicString(this ChangeRisk s) => s switch
    {
        ChangeRisk.Low => "منخفض",
        ChangeRisk.Medium => "متوسط",
        ChangeRisk.High => "عالي",
        ChangeRisk.Critical => "حرج",
        _ => s.ToString()
    };

    public static string ToArabicString(this ChangeApprovalStatus s) => s switch
    {
        ChangeApprovalStatus.Pending => "قيد المراجعة",
        ChangeApprovalStatus.Approved => "معتمد",
        ChangeApprovalStatus.Rejected => "مرفوض",
        ChangeApprovalStatus.Cancelled => "ملغي",
        _ => s.ToString()
    };

    public static string ToArabicString(this ChangeStatus s) => s switch
    {
        ChangeStatus.Draft => "مسودة",
        ChangeStatus.Submitted => "مقدم",
        ChangeStatus.Approved => "معتمد",
        ChangeStatus.Scheduled => "مجدول",
        ChangeStatus.Implementing => "قيد التنفيذ",
        ChangeStatus.Testing => "قيد الاختبار",
        ChangeStatus.Completed => "مكتمل",
        ChangeStatus.RolledBack => "تم التراجع",
        ChangeStatus.Cancelled => "ملغي",
        _ => s.ToString()
    };

    public static string ToArabicString(this ProblemStatus s) => s switch
    {
        ProblemStatus.Logged => "مسجل",
        ProblemStatus.Investigating => "قيد التحقيق",
        ProblemStatus.RootCauseIdentified => "تم تحديد السبب الجذري",
        ProblemStatus.WorkaroundKnown => "حل بديل معروف",
        ProblemStatus.Resolved => "تم الحل",
        ProblemStatus.Closed => "مغلق",
        _ => s.ToString()
    };

    public static string ToArabicString(this KnownErrorStatus s) => s switch
    {
        KnownErrorStatus.Active => "نشط",
        KnownErrorStatus.WorkaroundAvailable => "حل بديل متاح",
        KnownErrorStatus.Resolved => "تم الحل",
        KnownErrorStatus.Closed => "مغلق",
        _ => s.ToString()
    };

    public static string ToArabicString(this CIStatus s) => s switch
    {
        CIStatus.Operational => "تشغيلي",
        CIStatus.Degraded => "متدهور",
        CIStatus.OutOfService => "خارج الخدمة",
        CIStatus.Retired => "متقاعد",
        _ => s.ToString()
    };

    public static string ToArabicString(this ServiceRequestType s) => s switch
    {
        ServiceRequestType.SoftwareInstallation => "تثبيت برنامج",
        ServiceRequestType.PrinterInstallation => "تثبيت طابعة",
        ServiceRequestType.DomainJoin => "إدخال في النطاق",
        ServiceRequestType.PasswordReset => "إعادة تعيين كلمة المرور",
        ServiceRequestType.PermissionRequest => "طلب صلاحيات",
        ServiceRequestType.SharedFolderRequest => "طلب مجلد مشترك",
        ServiceRequestType.NewUserRequest => "مستخدم جديد",
        ServiceRequestType.EmailRequest => "طلب بريد",
        ServiceRequestType.HardwareRequest => "طلب معدات",
        ServiceRequestType.ApplicationAccess => "وصول لتطبيق",
        ServiceRequestType.Other => "أخرى",
        _ => s.ToString()
    };

    public static string ToArabicString(this ServiceRequestStatus s) => s switch
    {
        ServiceRequestStatus.New => "جديد",
        ServiceRequestStatus.Submitted => "مقدم",
        ServiceRequestStatus.Approved => "معتمد",
        ServiceRequestStatus.InProgress => "قيد التنفيذ",
        ServiceRequestStatus.Completed => "مكتمل",
        ServiceRequestStatus.Cancelled => "ملغي",
        ServiceRequestStatus.Rejected => "مرفوض",
        _ => s.ToString()
    };

    public static string ToArabicString(this NotificationType s) => s switch
    {
        NotificationType.Assignment => "تعيين",
        NotificationType.Transfer => "تحويل",
        NotificationType.Escalation => "تصعيد",
        NotificationType.SlaWarning => "تحذير SLA",
        NotificationType.SlaBreach => "خرق SLA",
        NotificationType.NewTicket => "تذكرة جديدة",
        NotificationType.ResolvedTicket => "تم حل التذكرة",
        NotificationType.ChangeApproval => "اعتماد تغيير",
        NotificationType.ProblemUpdate => "تحديث مشكلة",
        _ => s.ToString()
    };

    public static string ToArabicString(this EscalationLevel s) => s switch
    {
        EscalationLevel.None => "بدون",
        EscalationLevel.Level1 => "المستوى 1",
        EscalationLevel.Level2 => "المستوى 2",
        EscalationLevel.Level3 => "المستوى 3",
        EscalationLevel.Management => "الإدارة",
        _ => s.ToString()
    };
}
