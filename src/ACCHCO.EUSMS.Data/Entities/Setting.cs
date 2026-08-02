namespace ACCHCO.EUSMS.Data.Entities;

public class Setting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GroupName { get; set; }
    public string DataType { get; set; } = "string";
    public bool IsEncrypted { get; set; }
}
