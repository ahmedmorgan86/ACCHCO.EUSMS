namespace ACCHCO.EUSMS.Data.Entities;

public class Equipment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public EquipmentType Type { get; set; } = EquipmentType.PC;
    public string? AssetTag { get; set; }
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? ComputerName { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Location { get; set; }
    public string? Section { get; set; }
    public string? AssignedTo { get; set; }
    public string? Department { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Active;
    public string? BarcodeNumber { get; set; }
    public string? SubnetMask { get; set; }
    public string? DefaultGateway { get; set; }
    public string? DNSServers { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public enum EquipmentStatus
{
    Active = 1,
    Inactive = 2,
    UnderMaintenance = 3,
    Lost = 4
}
