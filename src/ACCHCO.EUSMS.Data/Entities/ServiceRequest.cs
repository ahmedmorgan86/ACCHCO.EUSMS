namespace ACCHCO.EUSMS.Data.Entities;

public class ServiceRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public ServiceRequestType RequestType { get; set; }
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.New;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterSection { get; set; } = string.Empty;
    public string? RequesterEmail { get; set; }
    public string? RequesterPhone { get; set; }
    public int? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public string? ApproverName { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? ApprovalNotes { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? CompletionNotes { get; set; }
    public string? RequiredForUser { get; set; }
    public string? RequiredForDepartment { get; set; }
    public string? SoftwareName { get; set; }
    public string? HardwareSpecs { get; set; }
    public string? FolderPath { get; set; }
    public string? Permissions { get; set; }

    public AppUser? Requester { get; set; }
    public AppUser? AssignedTo { get; set; }
}
