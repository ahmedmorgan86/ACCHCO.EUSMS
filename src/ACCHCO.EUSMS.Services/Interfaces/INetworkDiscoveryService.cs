namespace ACCHCO.EUSMS.Services.Interfaces;

public interface INetworkDiscoveryService
{
    Task<List<DiscoveredDevice>> ScanNetworkAsync(string subnet, IProgress<int>? progress = null);
    Task<Dictionary<string, string>> GetArpTableAsync();
}

public class DiscoveredDevice
{
    public string Hostname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? MacAddress { get; set; }
    public string? Manufacturer { get; set; }
    public bool IsOnline { get; set; }
    public string? DeviceCategory { get; set; }
}
