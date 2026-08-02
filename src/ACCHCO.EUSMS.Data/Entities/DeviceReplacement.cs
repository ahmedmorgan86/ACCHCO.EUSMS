namespace ACCHCO.EUSMS.Data.Entities;

public class DeviceReplacement : BaseEntity
{
    public int OldEquipmentId { get; set; }
    public int NewEquipmentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ReplacementDate { get; set; } = DateTime.Now;

    public Equipment OldEquipment { get; set; } = null!;
    public Equipment NewEquipment { get; set; } = null!;
}
