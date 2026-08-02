namespace ACCHCO.EUSMS.Data.Entities;

public class NetworkDevice : BaseEntity
{
    public string Hostname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? MacAddress { get; set; }
    public string? Manufacturer { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public string? DeviceCategory { get; set; }
    public string? OUPrefix { get; set; }
    public string? Notes { get; set; }
    public int? LinkedEquipmentId { get; set; }
    public Equipment? LinkedEquipment { get; set; }
}

public enum DeviceCategory
{
    All = 0,
    Computer = 1,
    Printer = 2,
    AccessPoint = 3,
    Switch = 4,
    Camera = 5,
    Phone = 6,
    Server = 7,
    RDT = 8,
    Other = 9
}
