namespace ACCHCO.EUSMS.Data.Entities;

public class Ticket : BaseEntity
{
    public string TicketNumber { get; set; } = string.Empty;
    public DateTime TicketDate { get; set; } = DateTime.Today;
    public TimeSpan TicketTime { get; set; } = DateTime.Now.TimeOfDay;
    public Shift Shift { get; set; } = Shift.Red;
    public int? SupportSpecialistId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterSection { get; set; } = string.Empty;
    public string? RequesterEmail { get; set; }
    public string? RequesterPhone { get; set; }
    public string? Location { get; set; }
    public EquipmentType EquipmentType { get; set; } = EquipmentType.PC;
    public string? EquipmentName { get; set; }
    public string? AssetTag { get; set; }
    public string? ComputerName { get; set; }
    public string? IpAddress { get; set; }
    public string? OperatingSystem { get; set; }
    public FaultType FaultType { get; set; } = FaultType.Hardware;
    public Priority Priority { get; set; } = Priority.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public string ProblemDescription { get; set; } = string.Empty;
    public string? ActionsTaken { get; set; }
    public string? Solution { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? FinishTime { get; set; }
    public double? ResolutionTimeMinutes { get; set; }
    public string? Notes { get; set; }
    public bool IsUrgent { get; set; }

    public AppUser? SupportSpecialist { get; set; }
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
}
