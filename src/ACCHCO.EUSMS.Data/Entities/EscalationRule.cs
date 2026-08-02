namespace ACCHCO.EUSMS.Data.Entities;

public class EscalationRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Priority? Priority { get; set; }
    public string? Category { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public int WarningMinutes { get; set; }
    public int BreachMinutes { get; set; }
    public EscalationLevel TargetLevel { get; set; }
    public string? TargetRole { get; set; }
    public bool IsActive { get; set; } = true;
    public string? NotificationMessage { get; set; }
}
