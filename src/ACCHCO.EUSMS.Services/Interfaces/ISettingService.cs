using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Services.Interfaces;

public interface ISettingService
{
    Task<string?> GetAsync(string key);
    Task<string> GetOrDefaultAsync(string key, string defaultValue);
    Task SetAsync(string key, string value, string? description = null, string? group = null);
    Task<Dictionary<string, string>> GetAllAsync();
    Task<IEnumerable<Setting>> GetAllEntitiesAsync();
    Task InitializeDefaultSettingsAsync();
}
