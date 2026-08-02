using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Services.Interfaces;

public interface IEquipmentService
{
    Task<Equipment?> GetByIdAsync(int id);
    Task<IEnumerable<Equipment>> GetAllAsync();
    Task<IEnumerable<Equipment>> GetFilteredAsync(EquipmentType? type, string? location,
        string? section, EquipmentStatus? status, string? search);
    Task<Equipment> CreateAsync(Equipment equipment, string currentUser);
    Task<Equipment> UpdateAsync(Equipment equipment, string currentUser);
    Task DeleteAsync(int equipmentId, string currentUser);
    Task<int> GetCountByTypeAsync(EquipmentType type);
    Task<Dictionary<EquipmentType, int>> GetCountByAllTypesAsync();
}
