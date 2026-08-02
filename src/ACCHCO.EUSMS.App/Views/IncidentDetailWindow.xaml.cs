using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ACCHCO.EUSMS.App.ViewModels;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.App.Views;

public partial class IncidentDetailWindow : Window
{
    public IncidentDetailWindow(int? incidentId = null)
    {
        InitializeComponent();

        var vm = App.ServiceProvider!.GetRequiredService<IncidentManagementViewModel>();
        DataContext = vm;

        if (incidentId.HasValue)
        {
            var incidentService = App.ServiceProvider!.GetRequiredService<IIncidentService>();
            var incident = incidentService.GetByIdAsync(incidentId.Value).GetAwaiter().GetResult();
            if (incident != null)
            {
                vm.EditIncident = new Incident
                {
                    Id = incident.Id,
                    IncidentNumber = incident.IncidentNumber,
                    RequesterName = incident.RequesterName,
                    RequesterSection = incident.RequesterSection,
                    ProblemDescription = incident.ProblemDescription,
                    ImpactDescription = incident.ImpactDescription,
                    Category = incident.Category,
                    Severity = incident.Severity,
                    Impact = incident.Impact,
                    Urgency = incident.Urgency,
                    Priority = incident.Priority,
                    Source = incident.Source,
                    EquipmentType = incident.EquipmentType,
                    EquipmentName = incident.EquipmentName,
                    Location = incident.Location,
                    ComputerName = incident.ComputerName,
                    IpAddress = incident.IpAddress,
                    AssignedSpecialistId = incident.AssignedSpecialistId,
                    WorkflowStatus = incident.WorkflowStatus,
                    Shift = incident.Shift
                };
                vm.IsEditMode = true;
            }
        }
    }
}
