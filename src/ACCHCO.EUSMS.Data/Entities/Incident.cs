namespace ACCHCO.EUSMS.Data.Entities;

public class Incident : BaseEntity
{
    public string IncidentNumber { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public TimeSpan IncidentTime { get; set; }
    public Shift Shift { get; set; }
    public IncidentSeverity Severity { get; set; }
    public IncidentImpact Impact { get; set; }
    public IncidentUrgency Urgency { get; set; }
    public IncidentSource Source { get; set; }
    public WorkflowStatus WorkflowStatus { get; set; } = WorkflowStatus.New;
    public Priority Priority { get; set; }
    public int? TicketId { get; set; }
    public int? RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterSection { get; set; } = string.Empty;
    public string? RequesterEmail { get; set; }
    public string? RequesterPhone { get; set; }
    public int? AssignedSpecialistId { get; set; }
    public string? Category { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public string? EquipmentName { get; set; }
    public int? ConfigurationItemId { get; set; }
    public string? ComputerName { get; set; }
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public string? ImpactDescription { get; set; }
    public string? Workaround { get; set; }
    public string? ResolutionNotes { get; set; }
    public int? ProblemId { get; set; }
    public int? KnownErrorId { get; set; }
    public DateTime? AcknowledgedDate { get; set; }
    public DateTime? InvestigatingDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public double? ResolutionTimeMinutes { get; set; }
    public EscalationLevel EscalationLevel { get; set; } = EscalationLevel.None;
    public DateTime? LastEscalationCheck { get; set; }
    public bool IsDuplicate { get; set; }
    public int? DuplicateOfIncidentId { get; set; }
    public string? ClosureNotes { get; set; }
    public string? ClosureCode { get; set; }
    public bool CustomerSatisfied { get; set; }

    public AppUser? Requester { get; set; }
    public AppUser? AssignedSpecialist { get; set; }
    public ConfigurationItem? ConfigurationItem { get; set; }
    public Problem? Problem { get; set; }
    public KnownError? KnownError { get; set; }
    public Ticket? Ticket { get; set; }
    public ICollection<IncidentHistory> Histories { get; set; } = new List<IncidentHistory>();
    public ICollection<IncidentComment> Comments { get; set; } = new List<IncidentComment>();
}
