namespace ACCHCO.EUSMS.Data.Entities;

public class IncidentHistory : BaseEntity
{
    public int IncidentId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime ChangedDate { get; set; }
    public string? ChangeDescription { get; set; }
    public Incident? Incident { get; set; }
}
