namespace ACCHCO.EUSMS.Data.Entities;

public class SlaDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public string? Category { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public string? Department { get; set; }
    public double ResponseTimeMinutes { get; set; }
    public double ResolutionTimeMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}
