using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.App.Views;

public partial class EquipmentDetailWindow : Window
{
    public EquipmentDetailWindow(int? equipmentId = null)
    {
        InitializeComponent();

        var vm = App.ServiceProvider!.GetRequiredService<EquipmentViewModel>();
        DataContext = vm;

        if (equipmentId.HasValue)
        {
            var service = App.ServiceProvider!.GetRequiredService<IEquipmentService>();
            var equipment = service.GetByIdAsync(equipmentId.Value).GetAwaiter().GetResult();
            if (equipment != null)
            {
                vm.EditEquipment = new Equipment
                {
                    Id = equipment.Id,
                    Name = equipment.Name,
                    Type = equipment.Type,
                    AssetTag = equipment.AssetTag,
                    SerialNumber = equipment.SerialNumber,
                    Manufacturer = equipment.Manufacturer,
                    Model = equipment.Model,
                    ComputerName = equipment.ComputerName,
                    IpAddress = equipment.IpAddress,
                    OperatingSystem = equipment.OperatingSystem,
                    Location = equipment.Location,
                    Section = equipment.Section,
                    AssignedTo = equipment.AssignedTo,
                    Status = equipment.Status,
                    PurchaseDate = equipment.PurchaseDate,
                    WarrantyExpiry = equipment.WarrantyExpiry,
                    Notes = equipment.Notes
                };
                vm.IsEditMode = true;
            }
        }
        else
        {
            vm.EditEquipment = new Equipment();
            vm.IsNewEquipment = true;
        }
    }
}
