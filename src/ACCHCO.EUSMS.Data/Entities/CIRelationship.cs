namespace ACCHCO.EUSMS.Data.Entities;

public class CIRelationship : BaseEntity
{
    public int SourceCiId { get; set; }
    public int TargetCiId { get; set; }
    public string RelationshipType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public ConfigurationItem? SourceCi { get; set; }
    public ConfigurationItem? TargetCi { get; set; }
}
