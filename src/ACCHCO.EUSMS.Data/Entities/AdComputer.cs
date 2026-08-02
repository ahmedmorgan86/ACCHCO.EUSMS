namespace ACCHCO.EUSMS.Data.Entities;

public class AdComputer : BaseEntity
{
    public string ComputerName { get; set; } = string.Empty;
    public string? DistinguishedName { get; set; }
    public string? OperatingSystem { get; set; }
    public string? IpAddress { get; set; }
    public string? Description { get; set; }
    public string? ManagedBy { get; set; }
    public string? OuPath { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastLogon { get; set; }
    public string? AdSid { get; set; }
    public DateTime LastSyncDate { get; set; } = DateTime.Now;
    public string? WhenCreated { get; set; }
    public string? WhenChanged { get; set; }
}
