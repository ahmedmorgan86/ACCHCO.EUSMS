using Microsoft.EntityFrameworkCore;
using ACCHCO.EUSMS.Data.Context;
using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Data.Repositories;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<Ticket?> GetByTicketNumberAsync(string ticketNumber);
    Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status);
    Task<IEnumerable<Ticket>> GetByShiftAsync(Shift shift);
    Task<IEnumerable<Ticket>> GetBySpecialistIdAsync(int specialistId);
    Task<string> GenerateNextTicketNumberAsync();
    Task<string> PeekNextTicketNumberAsync();
    Task<Ticket?> GetWithDetailsAsync(int id);
    Task<IEnumerable<Ticket>> GetOpenTicketsAsync();
    Task<int> GetTodayTicketCountAsync();
    Task<int> GetOpenCountAsync();
    Task<int> GetClosedCountAsync();
}

public class TicketRepository : Repository<Ticket>, ITicketRepository
{
    public TicketRepository(EusmsDbContext context) : base(context) { }

    public async Task<Ticket?> GetByTicketNumberAsync(string ticketNumber)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);
    }

    public override async Task<IEnumerable<Ticket>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking()
            .Include(t => t.SupportSpecialist)
            .OrderByDescending(t => t.TicketDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _dbSet.AsNoTracking()
            .Where(t => t.TicketDate >= from && t.TicketDate <= to)
            .OrderByDescending(t => t.TicketDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status)
    {
        return await _dbSet.AsNoTracking()
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.TicketDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetByShiftAsync(Shift shift)
    {
        return await _dbSet.AsNoTracking()
            .Where(t => t.Shift == shift)
            .OrderByDescending(t => t.TicketDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ticket>> GetBySpecialistIdAsync(int specialistId)
    {
        return await _dbSet.AsNoTracking()
            .Where(t => t.SupportSpecialistId == specialistId)
            .OrderByDescending(t => t.TicketDate)
            .ToListAsync();
    }

    public async Task<string> GenerateNextTicketNumberAsync()
    {
        return await GenerateSequentialNumberAsync("Ticket", "TK-", nameof(Ticket.TicketNumber));
    }

    public async Task<string> PeekNextTicketNumberAsync()
    {
        var today = DateTime.Today;
        var prefix = $"TK-{today:yyyyMMdd}-";
        var lastTicket = await _dbSet.AsNoTracking()
            .Where(t => t.TicketNumber.StartsWith(prefix))
            .OrderByDescending(t => t.TicketNumber)
            .FirstOrDefaultAsync();

        if (lastTicket == null)
            return $"{prefix}001";

        var lastNumber = int.Parse(lastTicket.TicketNumber.Split('-').Last());
        return $"{prefix}{(lastNumber + 1):D3}";
    }

    public async Task<Ticket?> GetWithDetailsAsync(int id)
    {
        return await _context.Tickets
            .Include(t => t.SupportSpecialist)
            .Include(t => t.Attachments)
            .Include(t => t.History.OrderByDescending(h => h.ChangedDate))
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Ticket>> GetOpenTicketsAsync()
    {
        return await _dbSet.AsNoTracking()
            .Where(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
            .OrderByDescending(t => t.TicketDate)
            .ToListAsync();
    }

    public async Task<int> GetTodayTicketCountAsync()
    {
        var today = DateTime.Today;
        return await _dbSet.CountAsync(t => t.TicketDate == today);
    }

    public async Task<int> GetOpenCountAsync()
    {
        return await _dbSet.CountAsync(t =>
            t.Status == TicketStatus.Open ||
            t.Status == TicketStatus.InProgress ||
            t.Status == TicketStatus.OnHold);
    }

    public async Task<int> GetClosedCountAsync()
    {
        return await _dbSet.CountAsync(t =>
            t.Status == TicketStatus.Resolved ||
            t.Status == TicketStatus.Closed);
    }
}

public interface IEquipmentRepository : IRepository<Equipment>
{
    Task<Equipment?> GetByAssetTagAsync(string assetTag);
    Task<IEnumerable<Equipment>> GetByTypeAsync(EquipmentType type);
    Task<IEnumerable<Equipment>> GetByLocationAsync(string location);
}

public class EquipmentRepository : Repository<Equipment>, IEquipmentRepository
{
    public EquipmentRepository(EusmsDbContext context) : base(context) { }

    public async Task<Equipment?> GetByAssetTagAsync(string assetTag)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(e => e.AssetTag == assetTag);
    }

    public async Task<IEnumerable<Equipment>> GetByTypeAsync(EquipmentType type)
    {
        return await _dbSet.AsNoTracking()
            .Where(e => e.Type == type)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetByLocationAsync(string location)
    {
        return await _dbSet.AsNoTracking()
            .Where(e => e.Location != null && e.Location.Contains(location))
            .ToListAsync();
    }
}

public interface IUserRepository : IRepository<AppUser>
{
    Task<AppUser?> GetByUsernameAsync(string username);
    Task<IEnumerable<AppUser>> GetActiveUsersAsync();
    Task<IEnumerable<AppUser>> GetByRoleAsync(UserRole role);
}

public class UserRepository : Repository<AppUser>, IUserRepository
{
    public UserRepository(EusmsDbContext context) : base(context) { }

    public async Task<AppUser?> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<IEnumerable<AppUser>> GetActiveUsersAsync()
    {
        return await _dbSet.Where(u => u.IsActive && !u.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<AppUser>> GetByRoleAsync(UserRole role)
    {
        return await _dbSet.Where(u => u.Role == role && u.IsActive && !u.IsDeleted).ToListAsync();
    }
}

public interface ISettingRepository : IRepository<Setting>
{
    Task<Setting?> GetByKeyAsync(string key);
    Task<Dictionary<string, string>> GetAllSettingsAsync();
}

public class SettingRepository : Repository<Setting>, ISettingRepository
{
    public SettingRepository(EusmsDbContext context) : base(context) { }

    public async Task<Setting?> GetByKeyAsync(string key)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Key == key);
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync()
    {
        return await _dbSet.ToDictionaryAsync(s => s.Key, s => s.Value);
    }
}

public interface IShiftHandoverRepository : IRepository<ShiftHandover>
{
    Task<IEnumerable<ShiftHandover>> GetByDateAsync(DateTime date);
    Task<ShiftHandover?> GetLatestAsync();
    Task<ShiftHandover?> GetWithDetailsAsync(int id);
}

public class ShiftHandoverRepository : Repository<ShiftHandover>, IShiftHandoverRepository
{
    public ShiftHandoverRepository(EusmsDbContext context) : base(context) { }

    public async Task<IEnumerable<ShiftHandover>> GetByDateAsync(DateTime date)
    {
        return await _dbSet.AsNoTracking()
            .Where(h => h.HandoverDate.Date == date.Date)
            .OrderByDescending(h => h.HandoverDate)
            .ToListAsync();
    }

    public async Task<ShiftHandover?> GetLatestAsync()
    {
        return await _dbSet.AsNoTracking()
            .OrderByDescending(h => h.HandoverDate)
            .FirstOrDefaultAsync();
    }

    public async Task<ShiftHandover?> GetWithDetailsAsync(int id)
    {
        return await _context.ShiftHandovers
            .Include(h => h.FromSpecialist)
            .Include(h => h.ToSpecialist)
            .Include(h => h.HandoverTickets)
                .ThenInclude(ht => ht.Ticket)
            .FirstOrDefaultAsync(h => h.Id == id);
    }
}
