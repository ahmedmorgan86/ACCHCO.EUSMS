namespace ACCHCO.EUSMS.Data.Entities;

public class AppUser : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Section { get; set; }
    public string? Title { get; set; }
    public UserRole Role { get; set; } = UserRole.SupportSpecialist;
    public bool IsActive { get; set; } = true;
    public bool IsFromAd { get; set; }
    public string? AdSid { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime? LastPasswordChangeDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }

    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
    public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<ShiftHandover> HandoverFrom { get; set; } = new List<ShiftHandover>();
    public ICollection<ShiftHandover> HandoverTo { get; set; } = new List<ShiftHandover>();
}
