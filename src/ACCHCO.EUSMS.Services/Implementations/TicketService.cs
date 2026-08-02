using Microsoft.EntityFrameworkCore;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.Services.Implementations;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IRepository<TicketAttachment> _attachmentRepo;
    private readonly IAuditService _auditService;

    public TicketService(ITicketRepository ticketRepo, IRepository<TicketAttachment> attachmentRepo, IAuditService auditService)
    {
        _ticketRepo = ticketRepo;
        _attachmentRepo = attachmentRepo;
        _auditService = auditService;
    }

    public async Task<Ticket?> GetByIdAsync(int id) => await _ticketRepo.GetWithDetailsAsync(id);

    public async Task<Ticket?> GetByNumberAsync(string ticketNumber) =>
        await _ticketRepo.GetByTicketNumberAsync(ticketNumber);

    public async Task<IEnumerable<Ticket>> GetAllAsync() => await _ticketRepo.GetAllAsync();

    public async Task<IEnumerable<Ticket>> GetFilteredAsync(
        DateTime? from, DateTime? to, Shift? shift, int? specialistId,
        string? section, EquipmentType? equipmentType, FaultType? faultType,
        Priority? priority, TicketStatus? status)
    {
        IQueryable<Ticket> query = _ticketRepo.Query().Include(t => t.SupportSpecialist);

        if (from.HasValue)
            query = query.Where(t => t.TicketDate >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.TicketDate <= to.Value);
        if (shift.HasValue)
            query = query.Where(t => t.Shift == shift.Value);
        if (specialistId.HasValue)
            query = query.Where(t => t.SupportSpecialistId == specialistId.Value);
        if (!string.IsNullOrWhiteSpace(section))
            query = query.Where(t => t.RequesterSection == section);
        if (equipmentType.HasValue)
            query = query.Where(t => t.EquipmentType == equipmentType.Value);
        if (faultType.HasValue)
            query = query.Where(t => t.FaultType == faultType.Value);
        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        return await query.OrderByDescending(t => t.TicketDate).ToListAsync();
    }

    public async Task<Ticket> CreateAsync(Ticket ticket, string currentUser)
    {
        ticket.TicketNumber = await GenerateNextNumberAsync();
        ticket.CreatedBy = currentUser;
        ticket.StartTime = DateTime.Now;
        var created = await _ticketRepo.AddAsync(ticket);

        await _auditService.LogAsync("Create", "Ticket", created.Id.ToString(),
            newValues: $"Ticket {created.TicketNumber} created",
            userId: currentUser, username: currentUser);

        return created;
    }

    public async Task<Ticket> UpdateAsync(Ticket ticket, string currentUser)
    {
        var existing = await _ticketRepo.GetByIdAsync(ticket.Id);
        if (existing == null)
            throw new InvalidOperationException($"Ticket {ticket.Id} not found.");

        var changes = GetChanges(existing, ticket);
        ticket.ModifiedBy = currentUser;
        await _ticketRepo.UpdateAsync(ticket);

        if (changes.Any())
        {
            foreach (var change in changes)
            {
                await _auditService.LogAsync("Update", "Ticket", ticket.Id.ToString(),
                    oldValues: $"{change.FieldName}: {change.OldValue}",
                    newValues: $"{change.FieldName}: {change.NewValue}",
                    userId: currentUser, username: currentUser);
            }
        }

        return ticket;
    }

    public async Task<bool> UpdateStatusAsync(int ticketId, TicketStatus status, string currentUser)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket == null) return false;

        var oldStatus = ticket.Status;
        ticket.Status = status;
        ticket.ModifiedBy = currentUser;

        if (status == TicketStatus.Resolved || status == TicketStatus.Closed)
        {
            ticket.FinishTime = DateTime.Now;
            if (ticket.StartTime.HasValue)
                ticket.ResolutionTimeMinutes = (ticket.FinishTime.Value - ticket.StartTime.Value).TotalMinutes;
        }

        await _ticketRepo.UpdateAsync(ticket);
        await _auditService.LogAsync("StatusChange", "Ticket", ticketId.ToString(),
            oldValues: $"Status: {oldStatus}", newValues: $"Status: {status}",
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<bool> ResolveAsync(int ticketId, string solution, string currentUser)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket == null) return false;

        ticket.Status = TicketStatus.Resolved;
        ticket.Solution = solution;
        ticket.FinishTime = DateTime.Now;
        ticket.ModifiedBy = currentUser;

        if (ticket.StartTime.HasValue)
            ticket.ResolutionTimeMinutes = (ticket.FinishTime.Value - ticket.StartTime.Value).TotalMinutes;

        await _ticketRepo.UpdateAsync(ticket);
        await _auditService.LogAsync("Resolve", "Ticket", ticketId.ToString(),
            newValues: $"Ticket resolved: {solution}",
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<bool> CloseAsync(int ticketId, string currentUser)
    {
        return await UpdateStatusAsync(ticketId, TicketStatus.Closed, currentUser);
    }

    public async Task<string> GenerateNextNumberAsync() =>
        await _ticketRepo.GenerateNextTicketNumberAsync();

    public async Task<string> PeekNextNumberAsync() =>
        await _ticketRepo.PeekNextTicketNumberAsync();

    public async Task<int> GetTodayCountAsync() => await _ticketRepo.GetTodayTicketCountAsync();
    public async Task<int> GetOpenCountAsync() => await _ticketRepo.GetOpenCountAsync();
    public async Task<int> GetClosedCountAsync() => await _ticketRepo.GetClosedCountAsync();

    public async Task<double> GetAverageResolutionTimeAsync()
    {
        var tickets = await _ticketRepo.GetAllAsync();
        var resolved = tickets.Where(t => t.ResolutionTimeMinutes.HasValue && t.ResolutionTimeMinutes > 0);
        return resolved.Any() ? resolved.Average(t => t.ResolutionTimeMinutes!.Value) : 0;
    }

    public async Task<IEnumerable<Ticket>> GetOpenTicketsAsync() =>
        await _ticketRepo.GetOpenTicketsAsync();

    public async Task DeleteAsync(int ticketId, string currentUser)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket != null)
        {
            await _ticketRepo.SoftDeleteAsync(ticket);
            await _auditService.LogAsync("Delete", "Ticket", ticketId.ToString(),
                oldValues: $"Ticket {ticket.TicketNumber} deleted",
                userId: currentUser, username: currentUser);
        }
    }

    public async Task AddAttachmentAsync(int ticketId, string fileName, byte[] data, string contentType, string uploadedBy)
    {
        var ticket = await _ticketRepo.GetByIdAsync(ticketId);
        if (ticket == null) return;

        var attachment = new TicketAttachment
        {
            TicketId = ticketId,
            FileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}",
            OriginalFileName = fileName,
            ContentType = contentType,
            FileSize = data.Length,
            FileData = data,
            UploadedBy = uploadedBy,
            UploadedDate = DateTime.Now
        };

        await _attachmentRepo.AddAsync(attachment);
    }

    public async Task RemoveAttachmentAsync(int attachmentId, string currentUser)
    {
        var attachment = await _attachmentRepo.GetByIdAsync(attachmentId);
        if (attachment != null)
        {
            await _attachmentRepo.DeleteAsync(attachment);
        }
        await _auditService.LogAsync("RemoveAttachment", "TicketAttachment", attachmentId.ToString(),
            userId: currentUser, username: currentUser);
    }

    private List<(string FieldName, string? OldValue, string? NewValue)> GetChanges(Ticket old, Ticket updated)
    {
        var changes = new List<(string, string?, string?)>();

        if (old.Status != updated.Status) changes.Add(("Status", old.Status.ToString(), updated.Status.ToString()));
        if (old.Priority != updated.Priority) changes.Add(("Priority", old.Priority.ToString(), updated.Priority.ToString()));
        if (old.Shift != updated.Shift) changes.Add(("Shift", old.Shift.ToString(), updated.Shift.ToString()));
        if (old.RequesterName != updated.RequesterName) changes.Add(("Requester", old.RequesterName, updated.RequesterName));
        if (old.ProblemDescription != updated.ProblemDescription) changes.Add(("ProblemDescription", old.ProblemDescription, updated.ProblemDescription));
        if (old.ActionsTaken != updated.ActionsTaken) changes.Add(("ActionsTaken", old.ActionsTaken, updated.ActionsTaken));
        if (old.Solution != updated.Solution) changes.Add(("Solution", old.Solution, updated.Solution));
        if (old.Location != updated.Location) changes.Add(("Location", old.Location, updated.Location));
        if (old.FaultType != updated.FaultType) changes.Add(("FaultType", old.FaultType.ToString(), updated.FaultType.ToString()));
        if (old.EquipmentType != updated.EquipmentType) changes.Add(("EquipmentType", old.EquipmentType.ToString(), updated.EquipmentType.ToString()));

        return changes;
    }
}
