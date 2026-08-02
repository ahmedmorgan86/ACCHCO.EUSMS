using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ACCHCO.EUSMS.Data.Context;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;
using ACCHCO.EUSMS.Services.Interfaces;
using ACCHCO.EUSMS.App.Helpers;

namespace ACCHCO.EUSMS.App.ViewModels;

public partial class NetworkDevicesViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly INetworkDiscoveryService _networkService;
    private readonly IRepository<NetworkDevice> _deviceRepo;
    private readonly IRepository<Equipment> _equipmentRepo;
    private readonly EusmsDbContext _context;

    private static readonly string[] Subnets = { "172.17.10", "172.17.30", "172.17.20", "172.17.70" };

    public NetworkDevicesViewModel()
    {
        _networkService = App.ServiceProvider!.GetRequiredService<INetworkDiscoveryService>();
        _deviceRepo = App.ServiceProvider!.GetRequiredService<IRepository<NetworkDevice>>();
        _equipmentRepo = App.ServiceProvider!.GetRequiredService<IRepository<Equipment>>();
        _context = App.ServiceProvider!.GetRequiredService<EusmsDbContext>();
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
    }

    public void Receive(DataChangedMessage message)
    {
        _ = LoadDevicesAsync();
    }

    [ObservableProperty] private ObservableCollection<NetworkDevice> _devices = new();
    [ObservableProperty] private ObservableCollection<NetworkDevice> _filteredDevices = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _scanProgress;
    [ObservableProperty] private string _scanStatus = "جاهز للمسح";
    [ObservableProperty] private DeviceCategory _selectedCategory = DeviceCategory.All;
    [ObservableProperty] private NetworkDevice? _selectedDevice;
    [ObservableProperty] private Equipment? _linkableEquipment;

    [RelayCommand]
    private async Task LoadDevicesAsync()
    {
        IsLoading = true;
        try
        {
            var selectedId = SelectedDevice?.Id;
            var devices = await _deviceRepo.GetAllAsync();
            Devices = new ObservableCollection<NetworkDevice>(devices);
            ApplyFilter();
            if (selectedId.HasValue)
                SelectedDevice = FilteredDevices.FirstOrDefault(d => d.Id == selectedId.Value);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "فشل تحميل الأجهزة");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ScanNetworkAsync()
    {
        IsLoading = true;
        ScanProgress = 0;

        try
        {
            var totalSubnets = Subnets.Length;
            ScanStatus = $"جاري مسح {totalSubnets} شبكات...";

            var allDiscovered = new List<DiscoveredDevice>();
            var existingDevices = (await _deviceRepo.GetAllAsync()).ToList();
            var toUpdate = new List<NetworkDevice>();
            var toAdd = new List<NetworkDevice>();
            var matchedExisting = new HashSet<int>();

            for (var subnetIndex = 0; subnetIndex < Subnets.Length; subnetIndex++)
            {
                var subnet = Subnets[subnetIndex];
                var subnetProgress = new Progress<int>(p =>
                {
                    var adjustedProgress = (int)Math.Round(((subnetIndex * 100) + p) / (double)totalSubnets);
                    ScanProgress = Math.Clamp(adjustedProgress, 0, 100);
                });

                var discovered = await _networkService.ScanNetworkAsync(subnet, subnetProgress);
                allDiscovered.AddRange(discovered);
            }

            ScanProgress = 100;
            ScanStatus = "جاري حفظ النتائج...";

            foreach (var disc in allDiscovered)
            {
                var found = existingDevices.FirstOrDefault(d =>
                    d.IpAddress == disc.IpAddress || d.MacAddress == disc.MacAddress);

                if (found != null)
                {
                    found.Hostname = disc.Hostname;
                    found.MacAddress = disc.MacAddress;
                    found.Manufacturer = disc.Manufacturer;
                    found.IsOnline = true;
                    found.LastSeen = DateTime.Now;
                    found.DeviceCategory = disc.DeviceCategory;
                    toUpdate.Add(found);
                    matchedExisting.Add(found.Id);
                }
                else
                {
                    toAdd.Add(new NetworkDevice
                    {
                        Hostname = disc.Hostname,
                        IpAddress = disc.IpAddress,
                        MacAddress = disc.MacAddress,
                        Manufacturer = disc.Manufacturer,
                        IsOnline = true,
                        LastSeen = DateTime.Now,
                        DeviceCategory = disc.DeviceCategory
                    });
                }
            }

            foreach (var existing in existingDevices)
            {
                if (!matchedExisting.Contains(existing.Id))
                {
                    existing.IsOnline = false;
                    toUpdate.Add(existing);
                }
            }

            foreach (var device in toAdd)
                _context.NetworkDevices.Add(device);

            foreach (var device in toUpdate)
            {
                var tracked = _context.NetworkDevices.Local
                    .FirstOrDefault(d => d.Id == device.Id);
                if (tracked != null)
                {
                    _context.Entry(tracked).CurrentValues.SetValues(device);
                    _context.Entry(tracked).State = EntityState.Modified;
                }
                else
                {
                    _context.NetworkDevices.Attach(device);
                    _context.Entry(device).State = EntityState.Modified;
                }
            }

            await _context.SaveChangesAsync();

            var allDevices = await _context.NetworkDevices.AsNoTracking().ToListAsync();
            Devices = new ObservableCollection<NetworkDevice>(allDevices);
            ApplyFilter();

            var onlineCount = allDevices.Count(d => d.IsOnline);
            ScanStatus = $"اكتمل المسح - {onlineCount} جهاز متصل من أصل {allDevices.Count}";
            DialogHelper.ShowInfo($"تم مسح {totalSubnets} شبكات.\nتم العثور على {onlineCount} جهاز متصل من أصل {allDevices.Count} جهاز مسجل.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "فشل مسح الشبكة");
            ScanStatus = $"فشل المسح: {ex.Message}";
            DialogHelper.ShowError($"فشل مسح الشبكة: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void FilterByCategory()
    {
        ApplyFilter();
    }

    [RelayCommand]
    private async Task LinkToEquipmentAsync()
    {
        if (SelectedDevice == null || LinkableEquipment == null)
        {
            DialogHelper.ShowWarning("اختر جهازاً ومعدة أولاً.");
            return;
        }

        SelectedDevice.LinkedEquipmentId = LinkableEquipment.Id;
        await _deviceRepo.UpdateAsync(SelectedDevice);
        DialogHelper.ShowInfo($"تم ربط '{SelectedDevice.Hostname}' بـ '{LinkableEquipment.Name}'.");
    }

    [RelayCommand]
    private void CreateTicketForDevice()
    {
        if (SelectedDevice == null)
        {
            DialogHelper.ShowWarning("اختر جهازاً أولاً.");
            return;
        }
        WeakReferenceMessenger.Default.Send(new NavigateMessage("CreateTicket", SelectedDevice));
    }

    private void ApplyFilter()
    {
        var filtered = SelectedCategory == DeviceCategory.All
            ? Devices
            : new ObservableCollection<NetworkDevice>(
                Devices.Where(d => d.DeviceCategory == SelectedCategory.ToString()));

        FilteredDevices = new ObservableCollection<NetworkDevice>(filtered);
    }

    partial void OnSelectedCategoryChanged(DeviceCategory value)
    {
        ApplyFilter();
    }
}
