using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.Services.Implementations;

public class NetworkDiscoveryService : INetworkDiscoveryService
{
    private static readonly Dictionary<string, string> OuiPrefixes = new()
    {
        { "D0-8E-79", "Dell" },
        { "00-1E-4F", "Dell" },
        { "18-66-DA", "Dell" },
        { "B8-2A-72", "Dell" },
        { "00-0C-29", "VMware" },
        { "00-50-56", "VMware" },
        { "00-15-5D", "Hyper-V" },
        { "08-00-27", "VirtualBox" },
        { "00-1A-2B", "Cisco" },
        { "00-1B-0D", "Cisco" },
        { "00-26-0B", "Cisco" },
        { "64-F6-9D", "Cisco" },
        { "E4-57-18", "Cisco" },
        { "D4-D2-52", "Cisco" },
        { "00-1F-7B", "Intel" },
        { "F8-63-3F", "Intel" },
        { "3C-22-FB", "Apple" },
        { "A4-83-E7", "Apple" },
        { "00-1D-D8", "Hewlett Packard" },
        { "00-17-A4", "Hewlett Packard" },
        { "3C-4A-92", "Hewlett Packard" },
        { "C8-5B-76", "Brother" },
        { "00-80-91", "Brother" },
        { "00-23-7D", "Zebra" },
        { "00-07-4D", "Zebra" },
        { "00-1F-A0", "Siemens" },
        { "00-0B-38", "Siemens" },
        { "00-A0-69", "Motorola" },
        { "00-1A-3F", "Motorola" },
        { "00-21-B7", "Ralink" },
        { "10-0C-6B", "Netgear" },
        { "C4-04-15", "Netgear" },
        { "20-E5-2A", "Netgear" },
        { "84-1B-5E", "Huawei" },
        { "00-46-4B", "Huawei" },
        { "48-46-C1", "Huawei" },
        { "00-0E-8F", "ZTE" },
        { "00-19-CB", "ZTE" },
        { "58-2A-F7", "Ubiquiti" },
        { "F4-E2-C6", "Ubiquiti" },
        { "B4-FB-E4", "Ubiquiti" },
        { "24-A4-3C", "TP-Link" },
        { "EC-08-6B", "TP-Link" },
        { "50-C7-BF", "TP-Link" },
        { "00-0E-58", "Hikvision" },
        { "28-57-BE", "Hikvision" },
        { "54-C4-15", "Hikvision" },
        { "40-16-7E", "Hikvision" },
        { "00-15-62", "Axis" },
        { "00-40-8C", "Axis" },
        { "AC-CC-8E", "Axis" },
    };

    private static readonly Ping PingClient = new();
    private static readonly int TimeoutMs = 1000;
    private static readonly int MaxConcurrency = 100;

    public async Task<List<DiscoveredDevice>> ScanNetworkAsync(string subnet, IProgress<int>? progress = null)
    {
        var devices = new ConcurrentBag<DiscoveredDevice>();
        var arpTable = await GetArpTableAsync();
        var totalHosts = 254;
        var completed = 0;
        var semaphore = new SemaphoreSlim(MaxConcurrency);

        // 1. Ping sweep across hosts 1 to 254
        var tasks = Enumerable.Range(1, 254).Select(i => ScanHostAsync(subnet, i, arpTable, semaphore, devices, totalHosts, () => Interlocked.Increment(ref completed), progress));
        await Task.WhenAll(tasks);

        // 2. Also ensure any device in the ARP table for this subnet is included (bypassing ICMP firewalls)
        foreach (var kvp in arpTable)
        {
            var ip = kvp.Key;
            if (ip.StartsWith(subnet + "."))
            {
                if (!devices.Any(d => d.IpAddress == ip))
                {
                    var mac = kvp.Value;
                    var manufacturer = GetManufacturer(mac);
                    var hostname = ip;
                    try
                    {
                        var entry = await Dns.GetHostEntryAsync(ip);
                        hostname = entry.HostName;
                    }
                    catch { }
                    var category = GuessCategory(hostname, manufacturer);

                    devices.Add(new DiscoveredDevice
                    {
                        Hostname = hostname,
                        IpAddress = ip,
                        MacAddress = mac,
                        Manufacturer = manufacturer,
                        IsOnline = true,
                        DeviceCategory = category
                    });
                }
            }
        }

        return devices
            .OrderBy(d => ParseIpOrder(d.IpAddress))
            .ToList();
    }

    private static async Task ScanHostAsync(string subnet, int host, Dictionary<string, string> arpTable, SemaphoreSlim semaphore, ConcurrentBag<DiscoveredDevice> devices, int totalHosts, Func<int> incrementCompleted, IProgress<int>? progress)
    {
        await semaphore.WaitAsync();
        try
        {
            var ip = $"{subnet}.{host}";
            var isOnline = false;

            try
            {
                var reply = await PingClient.SendPingAsync(ip, TimeoutMs);
                isOnline = reply.Status == IPStatus.Success;
            }
            catch { }

            if (isOnline)
            {
                var hostname = ip;
                try
                {
                    var entry = await Dns.GetHostEntryAsync(ip);
                    hostname = entry.HostName;
                }
                catch { }

                var mac = arpTable.GetValueOrDefault(ip);
                var manufacturer = GetManufacturer(mac);
                var category = GuessCategory(hostname, manufacturer);

                devices.Add(new DiscoveredDevice
                {
                    Hostname = hostname,
                    IpAddress = ip,
                    MacAddress = mac,
                    Manufacturer = manufacturer,
                    IsOnline = true,
                    DeviceCategory = category
                });
            }
        }
        finally
        {
            semaphore.Release();
            var completed = incrementCompleted();
            progress?.Report((int)((double)completed / totalHosts * 100));
        }
    }

    public async Task<Dictionary<string, string>> GetArpTableAsync()
    {
        var result = new Dictionary<string, string>();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "arp",
                Arguments = "-a",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.Default
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    var match = Regex.Match(line.Trim(),
                        @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\s+([0-9a-fA-F]{2}[-:][0-9a-fA-F]{2}[-:][0-9a-fA-F]{2}[-:][0-9a-fA-F]{2}[-:][0-9a-fA-F]{2}[-:][0-9a-fA-F]{2})");

                    if (match.Success)
                    {
                        var ip = match.Groups[1].Value;
                        var mac = match.Groups[2].Value.ToUpper();
                        result.TryAdd(ip, mac);
                    }
                }
            }
        }
        catch { }

        return result;
    }

    private static string? GetManufacturer(string? mac)
    {
        if (string.IsNullOrEmpty(mac)) return null;
        var prefix = mac.Replace(":", "-").ToUpper()[..8];
        return OuiPrefixes.TryGetValue(prefix, out var mfr) ? mfr : null;
    }

    private static string GuessCategory(string hostname, string? manufacturer)
    {
        var h = hostname.ToLowerInvariant();
        var m = (manufacturer ?? "").ToLowerInvariant();

        if (m.Contains("hikvision") || m.Contains("axis")) return "Camera";
        if (m.Contains("cisco") || m.Contains("ubiquiti") || m.Contains("tp-link") || m.Contains("netgear")) return "Switch";
        if (m.Contains("hp") || m.Contains("hewlett")) return "Printer";
        if (m.Contains("brother") || m.Contains("zebra")) return "Printer";
        if (m.Contains("siemens")) return "Other";

        if (h.Contains("printer") || h.Contains("prn") || h.Contains("lp")) return "Printer";
        if (h.Contains("cam") || h.Contains("ipc") || h.Contains("nvr")) return "Camera";
        if (h.Contains("ap-") || h.Contains("wifi") || h.Contains("access")) return "AccessPoint";
        if (h.Contains("sw-") || h.Contains("switch")) return "Switch";
        if (h.Contains("srv") || h.Contains("server") || h.Contains("dc-")) return "Server";
        if (h.Contains("rdt") || h.Contains("scanner") || h.Contains("rf")) return "RDT";
        if (h.Contains("phone") || h.Contains("ip-phone")) return "Phone";
        if (h.Contains("pc-") || h.Contains("ws-") || h.Contains("desktop")) return "Computer";
        if (h.Contains("laptop") || h.Contains("nb-")) return "Computer";

        return "Other";
    }

    private static long ParseIpOrder(string ip)
    {
        if (IPAddress.TryParse(ip, out var addr))
        {
            var bytes = addr.GetAddressBytes();
            return ((long)bytes[0] << 24) | ((long)bytes[1] << 16) | ((long)bytes[2] << 8) | bytes[3];
        }
        return 0;
    }
}
