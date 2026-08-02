namespace ACCHCO.EUSMS.Data.Entities;

public class KnownError : BaseEntity
{
    public string KnownErrorNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string? Workaround { get; set; }
    public string? PermanentSolution { get; set; }
    public KnownErrorStatus Status { get; set; } = KnownErrorStatus.Active;
    public EquipmentType? RelatedEquipmentType { get; set; }
    public string? RelatedSoftware { get; set; }
    public string? RelatedEquipment { get; set; }
    public int? ProblemId { get; set; }
    public int? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    public Problem? Problem { get; set; }
    public AppUser? CreatedUser { get; set; }
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}
