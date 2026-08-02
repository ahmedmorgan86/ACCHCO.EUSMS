using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Services.Interfaces;

public interface IShiftHandoverService
{
    Task<ShiftHandover?> GetByIdAsync(int id);
    Task<IEnumerable<ShiftHandover>> GetAllAsync();
    Task<ShiftHandover?> GetLatestAsync();
    Task<ShiftHandover?> GetLatestByShiftAsync(Shift shift);
    Task<ShiftHandover> CreateAsync(ShiftHandover handover, List<int> ticketIds, string currentUser);
    Task<bool> AcknowledgeAsync(int handoverId, string currentUser);
    Task<IEnumerable<Ticket>> GetPendingTicketsForHandoverAsync(Shift fromShift, DateTime date);
}
