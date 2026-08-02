using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Services.Interfaces;

public interface ITicketService
{
    Task<Ticket?> GetByIdAsync(int id);
    Task<Ticket?> GetByNumberAsync(string ticketNumber);
    Task<IEnumerable<Ticket>> GetAllAsync();
    Task<IEnumerable<Ticket>> GetFilteredAsync(DateTime? from, DateTime? to, Shift? shift,
        int? specialistId, string? section, EquipmentType? equipmentType,
        FaultType? faultType, Priority? priority, TicketStatus? status);
    Task<Ticket> CreateAsync(Ticket ticket, string currentUser);
    Task<Ticket> UpdateAsync(Ticket ticket, string currentUser);
    Task<bool> UpdateStatusAsync(int ticketId, TicketStatus status, string currentUser);
    Task<bool> ResolveAsync(int ticketId, string solution, string currentUser);
    Task<bool> CloseAsync(int ticketId, string currentUser);
    Task<string> GenerateNextNumberAsync();
    Task<string> PeekNextNumberAsync();
    Task<int> GetTodayCountAsync();
    Task<int> GetOpenCountAsync();
    Task<int> GetClosedCountAsync();
    Task<double> GetAverageResolutionTimeAsync();
    Task<IEnumerable<Ticket>> GetOpenTicketsAsync();
    Task DeleteAsync(int ticketId, string currentUser);
    Task AddAttachmentAsync(int ticketId, string fileName, byte[] data, string contentType, string uploadedBy);
    Task RemoveAttachmentAsync(int attachmentId, string currentUser);
}
