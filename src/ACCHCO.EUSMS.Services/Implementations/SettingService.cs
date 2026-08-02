using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.Services.Implementations;

public class SettingService : ISettingService
{
    private readonly ISettingRepository _repo;

    public SettingService(ISettingRepository repo)
    {
        _repo = repo;
    }

    public async Task<string?> GetAsync(string key)
    {
        var setting = await _repo.GetByKeyAsync(key);
        return setting?.Value;
    }

    public async Task<string> GetOrDefaultAsync(string key, string defaultValue)
    {
        var value = await GetAsync(key);
        return value ?? defaultValue;
    }

    public async Task SetAsync(string key, string value, string? description = null, string? group = null)
    {
        var existing = await _repo.GetByKeyAsync(key);
        if (existing != null)
        {
            existing.Value = value;
            if (description != null) existing.Description = description;
            if (group != null) existing.GroupName = group;
            await _repo.UpdateAsync(existing);
        }
        else
        {
            var setting = new Setting
            {
                Key = key,
                Value = value,
                Description = description,
                GroupName = group
            };
            await _repo.AddAsync(setting);
        }
    }

    public async Task<Dictionary<string, string>> GetAllAsync() =>
        await _repo.GetAllSettingsAsync();

    public async Task<IEnumerable<Setting>> GetAllEntitiesAsync() =>
        await _repo.GetAllAsync();

    public async Task InitializeDefaultSettingsAsync()
    {
        var defaults = new Dictionary<string, (string Value, string Description, string Group)>
        {
            ["CompanyName"] = ("Alexandria Container & Cargo Handling Company", "Company Name", "General"),
            ["CompanyNameAr"] = ("شركة الأسكندرية لتداول الحاويات و البضائع", "Company Name (Arabic)", "General"),
            ["DepartmentName"] = ("End User Support Department", "Department Name", "General"),
            ["AppTitle"] = ("ACCHCO EUSMS", "Application Title", "General"),
            ["TicketPrefix"] = ("TK", "Ticket Number Prefix", "Tickets"),
            ["DefaultPriority"] = ("Medium", "Default Ticket Priority", "Tickets"),
            ["DefaultShift"] = ("Red", "Default Shift", "Tickets"),
            ["AdSyncEnabled"] = ("true", "Enable AD Synchronization", "Active Directory"),
            ["AdDomain"] = (Environment.UserDomainName, "AD Domain Name", "Active Directory"),
            ["AdOuPath"] = ("", "AD OU Path (leave empty for all)", "Active Directory"),
            ["BackupPath"] = (@"Backups", "Backup Directory Path (relative to app folder)", "Backup"),
            ["MaxFileSize"] = ("10", "Max Upload File Size (MB)", "General"),
            ["ShowDashboardWidgets"] = ("true", "Show Dashboard Widgets", "Dashboard"),
            ["PrimaryColor"] = ("#FF1565C0", "Primary Theme Color", "Appearance"),
            ["AccentColor"] = ("#FF0D47A1", "Accent Theme Color", "Appearance"),
        };

        foreach (var kvp in defaults)
        {
            var existing = await _repo.GetByKeyAsync(kvp.Key);
            if (existing != null)
            {
                existing.Value = kvp.Value.Value;
                existing.Description = kvp.Value.Description;
                existing.GroupName = kvp.Value.Group;
                await _repo.UpdateAsync(existing);
            }
            else
            {
                await _repo.AddAsync(new Setting
                {
                    Key = kvp.Key,
                    Value = kvp.Value.Value,
                    Description = kvp.Value.Description,
                    GroupName = kvp.Value.Group,
                    DataType = "string"
                });
            }
        }
    }
}
