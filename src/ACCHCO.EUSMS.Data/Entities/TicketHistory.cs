namespace ACCHCO.EUSMS.Data.Entities;

public class TicketHistory : BaseEntity
{
    public int TicketId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedDate { get; set; } = DateTime.Now;
    public string? ChangeDescription { get; set; }

    public Ticket Ticket { get; set; } = null!;
}
