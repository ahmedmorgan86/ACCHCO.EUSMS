namespace ACCHCO.EUSMS.Data.Entities;

public class ConfigurationItem : BaseEntity
{
    public string CiNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public CIStatus Status { get; set; } = CIStatus.Operational;
    public string? SerialNumber { get; set; }
    public string? AssetTag { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? IpAddress { get; set; }
    public string? ComputerName { get; set; }
    public string? Location { get; set; }
    public string? Department { get; set; }
    public int? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string? Notes { get; set; }
    public int? LinkedEquipmentId { get; set; }

    public AppUser? Owner { get; set; }
    public Equipment? LinkedEquipment { get; set; }
    public ICollection<CIRelationship> ChildRelationships { get; set; } = new List<CIRelationship>();
    public ICollection<CIRelationship> ParentRelationships { get; set; } = new List<CIRelationship>();
}
