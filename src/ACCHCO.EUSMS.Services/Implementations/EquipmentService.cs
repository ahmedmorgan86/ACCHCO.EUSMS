using Microsoft.EntityFrameworkCore;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.Services.Implementations;

public class EquipmentService : IEquipmentService
{
    private readonly IEquipmentRepository _repo;
    private readonly IAuditService _auditService;

    public EquipmentService(IEquipmentRepository repo, IAuditService auditService)
    {
        _repo = repo;
        _auditService = auditService;
    }

    public async Task<Equipment?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);
    public async Task<IEnumerable<Equipment>> GetAllAsync() => await _repo.GetAllAsync();

    public async Task<IEnumerable<Equipment>> GetFilteredAsync(
        EquipmentType? type, string? location, string? section,
        EquipmentStatus? status, string? search)
    {
        var query = _repo.Query();

        if (type.HasValue)
            query = query.Where(e => e.Type == type.Value);
        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(e => e.Location != null && e.Location.Contains(location));
        if (!string.IsNullOrWhiteSpace(section))
            query = query.Where(e => e.Section == section);
        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Name.Contains(search) ||
                (e.AssetTag != null && e.AssetTag.Contains(search)) ||
                (e.ComputerName != null && e.ComputerName.Contains(search)));

        return await query.OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<Equipment> CreateAsync(Equipment equipment, string currentUser)
    {
        equipment.CreatedBy = currentUser;
        var created = await _repo.AddAsync(equipment);
        await _auditService.LogAsync("Create", "Equipment", created.Id.ToString(),
            newValues: $"Equipment '{created.Name}' created",
            userId: currentUser, username: currentUser);
        return created;
    }

    public async Task<Equipment> UpdateAsync(Equipment equipment, string currentUser)
    {
        equipment.ModifiedBy = currentUser;
        await _repo.UpdateAsync(equipment);
        await _auditService.LogAsync("Update", "Equipment", equipment.Id.ToString(),
            newValues: $"Equipment '{equipment.Name}' updated",
            userId: currentUser, username: currentUser);
        return equipment;
    }

    public async Task DeleteAsync(int equipmentId, string currentUser)
    {
        var equipment = await _repo.GetByIdAsync(equipmentId);
        if (equipment != null)
        {
            await _repo.SoftDeleteAsync(equipment);
            await _auditService.LogAsync("Delete", "Equipment", equipmentId.ToString(),
                oldValues: $"Equipment '{equipment.Name}' deleted",
                userId: currentUser, username: currentUser);
        }
    }

    public async Task<int> GetCountByTypeAsync(EquipmentType type) =>
        await _repo.CountAsync(e => e.Type == type);

    public async Task<Dictionary<EquipmentType, int>> GetCountByAllTypesAsync()
    {
        var all = await _repo.GetAllAsync();
        return all.GroupBy(e => e.Type)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
