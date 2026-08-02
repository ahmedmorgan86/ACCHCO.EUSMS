using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ACCHCO.EUSMS.Data.Context;
using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Data.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task SoftDeleteAsync(T entity);
    Task<int> SaveChangesAsync();
    IQueryable<T> Query();
    Task<string> GenerateSequentialNumberAsync(string seqKey, string numberPrefix, string numberColumn);
}

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly EusmsDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(EusmsDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate == null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        var local = _dbSet.Local.FirstOrDefault(e => e.Id == entity.Id);
        if (local != null && !ReferenceEquals(local, entity))
            _context.Entry(local).State = EntityState.Detached;
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        var local = _dbSet.Local.FirstOrDefault(e => e.Id == entity.Id);
        if (local != null && !ReferenceEquals(local, entity))
            _context.Entry(local).State = EntityState.Detached;
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task SoftDeleteAsync(T entity)
    {
        var local = _dbSet.Local.FirstOrDefault(e => e.Id == entity.Id);
        if (local != null && !ReferenceEquals(local, entity))
            _context.Entry(local).State = EntityState.Detached;
        entity.IsDeleted = true;
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public virtual async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public virtual IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }

    public virtual async Task<string> GenerateSequentialNumberAsync(string seqKey, string numberPrefix, string numberColumn)
    {
        var tableName = _context.Model.FindEntityType(typeof(T))!.GetTableName()!;
        var today = DateTime.Today;
        var dateKey = today.ToString("yyyyMMdd");
        var fullPrefix = $"{numberPrefix}{dateKey}-";
        var lastNumber = await SequenceNumberStore.NextAsync(_context, seqKey, dateKey, tableName, numberColumn, fullPrefix);
        return $"{fullPrefix}{lastNumber:D3}";
    }
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly EusmsDbContext _context;
    private readonly DbSet<AuditLog> _dbSet;

    public AuditLogRepository(EusmsDbContext context)
    {
        _context = context;
        _dbSet = context.Set<AuditLog>();
    }

    public async Task<AuditLog> AddAsync(AuditLog entity)
    {
        entity.Timestamp = DateTime.Now;
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().OrderByDescending(e => e.Timestamp).ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _dbSet.AsNoTracking()
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserAsync(string username)
    {
        return await _dbSet.AsNoTracking()
            .Where(e => e.Username == username)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }

    public IQueryable<AuditLog> Query() => _dbSet.AsQueryable();
}

public interface IAuditLogRepository
{
    Task<AuditLog> AddAsync(AuditLog entity);
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<IEnumerable<AuditLog>> GetByUserAsync(string username);
    IQueryable<AuditLog> Query();
}
