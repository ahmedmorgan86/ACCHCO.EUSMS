namespace ACCHCO.EUSMS.Data.Entities;

public class TicketAttachment : BaseEntity
{
    public int TicketId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public DateTime UploadedDate { get; set; } = DateTime.Now;
    public string? UploadedBy { get; set; }

    public Ticket Ticket { get; set; } = null!;
}
