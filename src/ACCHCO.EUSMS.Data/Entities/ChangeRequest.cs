namespace ACCHCO.EUSMS.Data.Entities;

public class ChangeRequest : BaseEntity
{
    public string ChangeNumber { get; set; } = string.Empty;
    public ChangeType ChangeType { get; set; }
    public ChangeStatus Status { get; set; } = ChangeStatus.Draft;
    public ChangeRisk RiskLevel { get; set; }
    public ChangeApprovalStatus ApprovalStatus { get; set; } = ChangeApprovalStatus.Pending;
    public int? RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterSection { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? AffectedCIs { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? ImplementationDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? RollbackPlan { get; set; }
    public string? TestingResult { get; set; }
    public string? ImplementationNotes { get; set; }
    public int? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public string? ApprovalNotes { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? ApproverName { get; set; }

    public AppUser? Requester { get; set; }
    public AppUser? AssignedTo { get; set; }
    public ICollection<ChangeHistory> Histories { get; set; } = new List<ChangeHistory>();
}
