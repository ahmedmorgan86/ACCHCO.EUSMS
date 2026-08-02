using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string? entityName = null, string? entityId = null,
        string? oldValues = null, string? newValues = null, string? details = null,
        string? userId = null, string? username = null);
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<IEnumerable<AuditLog>> GetByUserAsync(string username);
    IQueryable<AuditLog> Query();
}
