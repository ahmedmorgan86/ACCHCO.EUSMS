namespace ACCHCO.EUSMS.Data.Entities;

public class Problem : BaseEntity
{
    public string ProblemNumber { get; set; } = string.Empty;
    public ProblemStatus Status { get; set; } = ProblemStatus.Logged;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RootCause { get; set; }
    public string? Workaround { get; set; }
    public string? PermanentSolution { get; set; }
    public bool IsKnownError { get; set; }
    public int? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public Priority Priority { get; set; }
    public EquipmentType? RelatedEquipmentType { get; set; }
    public string? RelatedEquipmentName { get; set; }
    public DateTime? IdentifiedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public string? ResolutionNotes { get; set; }

    public AppUser? Owner { get; set; }
    public ICollection<ProblemIncident> ProblemIncidents { get; set; } = new List<ProblemIncident>();
    public ICollection<KnownError> KnownErrors { get; set; } = new List<KnownError>();
}
