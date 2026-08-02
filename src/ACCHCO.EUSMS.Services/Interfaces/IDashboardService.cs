using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Services.Interfaces;

public interface IDashboardService
{
    Task<int> GetTodayTicketCountAsync();
    Task<int> GetOpenTicketCountAsync();
    Task<int> GetClosedTicketCountAsync();
    Task<Dictionary<Shift, int>> GetTicketsByShiftAsync();
    Task<Dictionary<string, int>> GetTicketsBySpecialistAsync();
    Task<double> GetAverageResolutionTimeAsync();
    Task<Dictionary<FaultType, int>> GetMostCommonFaultsAsync();
    Task<Dictionary<EquipmentType, int>> GetFaultsByEquipmentTypeAsync();
    Task<Dictionary<EquipmentType, int>> GetEquipmentStatisticsAsync();
    Task<List<Ticket>> GetRecentTicketsAsync(int count = 10);
    Task<DashboardKpis> GetKpisAsync();
}

public class DashboardKpis
{
    public int TotalTicketsToday { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
    public double AverageResolutionMinutes { get; set; }
    public int TotalEquipment { get; set; }
    public int TotalUsers { get; set; }
    public double ResolutionRate { get; set; }
    public int CriticalOpenTickets { get; set; }
    public int HighOpenTickets { get; set; }
}
