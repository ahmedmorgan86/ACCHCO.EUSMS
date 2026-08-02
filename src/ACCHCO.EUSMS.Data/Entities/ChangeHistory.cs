namespace ACCHCO.EUSMS.Data.Entities;

public class ChangeHistory : BaseEntity
{
    public int ChangeRequestId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime ChangedDate { get; set; }
    public string? ChangeDescription { get; set; }
    public ChangeRequest? ChangeRequest { get; set; }
}
