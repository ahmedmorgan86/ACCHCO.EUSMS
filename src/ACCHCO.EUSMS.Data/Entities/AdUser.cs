namespace ACCHCO.EUSMS.Data.Entities;

public class AdUser : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? DistinguishedName { get; set; }
    public string? OuPath { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastLogon { get; set; }
    public string? AdSid { get; set; }
    public DateTime LastSyncDate { get; set; } = DateTime.Now;
    public string? WhenCreated { get; set; }
    public string? WhenChanged { get; set; }
    public string? Manager { get; set; }
    public string? Office { get; set; }
}
