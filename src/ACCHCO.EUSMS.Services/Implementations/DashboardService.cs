using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IEquipmentRepository _equipmentRepo;
    private readonly IUserRepository _userRepo;

    public DashboardService(
        ITicketRepository ticketRepo,
        IEquipmentRepository equipmentRepo,
        IUserRepository userRepo)
    {
        _ticketRepo = ticketRepo;
        _equipmentRepo = equipmentRepo;
        _userRepo = userRepo;
    }

    public async Task<int> GetTodayTicketCountAsync() => await _ticketRepo.GetTodayTicketCountAsync();
    public async Task<int> GetOpenTicketCountAsync() => await _ticketRepo.GetOpenCountAsync();
    public async Task<int> GetClosedTicketCountAsync() => await _ticketRepo.GetClosedCountAsync();

    public async Task<Dictionary<Shift, int>> GetTicketsByShiftAsync()
    {
        var tickets = await _ticketRepo.GetAllAsync();
        return tickets.GroupBy(t => t.Shift)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetTicketsBySpecialistAsync()
    {
        var tickets = await _ticketRepo.GetAllAsync();
        var users = await _userRepo.GetAllAsync();

        var result = users.ToDictionary(u => u.FullName, u => 0);

        var grouped = tickets.GroupBy(t => t.SupportSpecialistId);

        foreach (var group in grouped)
        {
            if (!group.Key.HasValue)
                result["غير مسند"] = group.Count();
            else
            {
                var user = users.FirstOrDefault(u => u.Id == group.Key.Value);
                if (user != null)
                    result[user.FullName] = group.Count();
                else
                    result["غير معروف"] = group.Count();
            }
        }

        return result;
    }

    public async Task<double> GetAverageResolutionTimeAsync()
    {
        var tickets = await _ticketRepo.GetAllAsync();
        var resolved = tickets.Where(t => t.ResolutionTimeMinutes.HasValue && t.ResolutionTimeMinutes > 0);
        return resolved.Any() ? resolved.Average(t => t.ResolutionTimeMinutes!.Value) : 0;
    }

    public async Task<Dictionary<FaultType, int>> GetMostCommonFaultsAsync()
    {
        var tickets = await _ticketRepo.GetAllAsync();
        return tickets.GroupBy(t => t.FaultType)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<EquipmentType, int>> GetFaultsByEquipmentTypeAsync()
    {
        var tickets = await _ticketRepo.GetAllAsync();
        return tickets.GroupBy(t => t.EquipmentType)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<EquipmentType, int>> GetEquipmentStatisticsAsync()
    {
        var equipment = await _equipmentRepo.GetAllAsync();
        return equipment.GroupBy(e => e.Type)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<Ticket>> GetRecentTicketsAsync(int count = 10)
    {
        var tickets = await _ticketRepo.GetAllAsync();
        return tickets.OrderByDescending(t => t.CreatedDate).Take(count).ToList();
    }

    public async Task<DashboardKpis> GetKpisAsync()
    {
        var totalTickets = await _ticketRepo.GetAllAsync();
        var todayCount = await GetTodayTicketCountAsync();
        var openCount = await GetOpenTicketCountAsync();
        var closedCount = await _ticketRepo.GetClosedCountAsync();
        var avgRes = await GetAverageResolutionTimeAsync();
        var equipCount = await _equipmentRepo.CountAsync();
        var userCount = await _userRepo.CountAsync();

        var total = openCount + closedCount;
        var critical = totalTickets.Count(t =>
            (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress) &&
            t.Priority == Priority.Critical);
        var high = totalTickets.Count(t =>
            (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress) &&
            t.Priority == Priority.High);

        return new DashboardKpis
        {
            TotalTicketsToday = todayCount,
            OpenTickets = openCount,
            ClosedTickets = closedCount,
            AverageResolutionMinutes = avgRes,
            TotalEquipment = equipCount,
            TotalUsers = userCount,
            ResolutionRate = total > 0 ? (double)closedCount / total * 100 : 0,
            CriticalOpenTickets = critical,
            HighOpenTickets = high
        };
    }
}
