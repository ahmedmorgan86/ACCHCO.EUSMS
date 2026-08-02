using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.Services.Implementations;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repo;

    public AuditService(IAuditLogRepository repo)
    {
        _repo = repo;
    }

    public async Task LogAsync(string action, string? entityName = null, string? entityId = null,
        string? oldValues = null, string? newValues = null, string? details = null,
        string? userId = null, string? username = null)
    {
        var log = new AuditLog
        {
            Timestamp = DateTime.Now,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            Details = details,
            UserId = userId,
            Username = username,
            MachineName = Environment.MachineName
        };

        try
        {
            await _repo.AddAsync(log);
        }
        catch
        {
            // Audit logging should not throw exceptions
        }
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync() => await _repo.GetAllAsync();

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to) =>
        await _repo.GetByDateRangeAsync(from, to);

    public async Task<IEnumerable<AuditLog>> GetByUserAsync(string username) =>
        await _repo.GetByUserAsync(username);

    public IQueryable<AuditLog> Query() => _repo.Query();
}
