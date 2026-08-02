using Microsoft.EntityFrameworkCore;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.Services.Implementations;

public class ShiftHandoverService : IShiftHandoverService
{
    private readonly IShiftHandoverRepository _repo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IAuditService _auditService;

    public ShiftHandoverService(
        IShiftHandoverRepository repo,
        ITicketRepository ticketRepo,
        IAuditService auditService)
    {
        _repo = repo;
        _ticketRepo = ticketRepo;
        _auditService = auditService;
    }

    public async Task<ShiftHandover?> GetByIdAsync(int id) => await _repo.GetWithDetailsAsync(id);
    public async Task<IEnumerable<ShiftHandover>> GetAllAsync() => await _repo.GetAllAsync();
    public async Task<ShiftHandover?> GetLatestAsync() => await _repo.GetLatestAsync();

    public async Task<ShiftHandover?> GetLatestByShiftAsync(Shift shift)
    {
        var all = await _repo.GetAllAsync();
        return all.OrderByDescending(h => h.HandoverDate).FirstOrDefault(h => h.ToShift == shift);
    }

    public async Task<ShiftHandover> CreateAsync(ShiftHandover handover, List<int> ticketIds, string currentUser)
    {
        handover.CreatedBy = currentUser;
        handover.TotalTicketsHandled = ticketIds.Count;

        foreach (var ticketId in ticketIds)
        {
            handover.HandoverTickets.Add(new ShiftHandoverTicket
            {
                TicketId = ticketId,
                IsPending = true
            });
        }

        var created = await _repo.AddAsync(handover);
        await _auditService.LogAsync("Create", "ShiftHandover", created.Id.ToString(),
            newValues: $"Handover from {created.FromShift} to {created.ToShift} created",
            userId: currentUser, username: currentUser);
        return created;
    }

    public async Task<bool> AcknowledgeAsync(int handoverId, string currentUser)
    {
        var handover = await _repo.GetByIdAsync(handoverId);
        if (handover == null) return false;

        handover.IsAcknowledged = true;
        handover.AcknowledgedDate = DateTime.Now;
        handover.ModifiedBy = currentUser;
        await _repo.UpdateAsync(handover);
        await _auditService.LogAsync("Acknowledge", "ShiftHandover", handoverId.ToString(),
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<IEnumerable<Ticket>> GetPendingTicketsForHandoverAsync(Shift fromShift, DateTime date)
    {
        var tickets = await _ticketRepo.Query()
            .Where(t => t.Shift == fromShift && t.TicketDate.Date == date.Date &&
                       (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress))
            .ToListAsync();
        return tickets;
    }
}
