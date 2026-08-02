namespace ACCHCO.EUSMS.Data.Entities;

public class Notification : BaseEntity
{
    public NotificationType NotificationType { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadDate { get; set; }
    public DateTime NotificationDate { get; set; }

    public AppUser? User { get; set; }
}
