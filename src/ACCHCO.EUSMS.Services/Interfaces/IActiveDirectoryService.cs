using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Services.Interfaces;

public interface IActiveDirectoryService
{
    bool IsAvailable();
    Task<IEnumerable<AdComputer>> SyncComputersAsync();
    Task<IEnumerable<AdUser>> SyncUsersAsync();
    Task<AdComputer?> FindComputerByNameAsync(string computerName);
    Task<AdUser?> FindUserByUsernameAsync(string username);
    Task<IEnumerable<AdComputer>> GetAllComputersAsync();
    Task<IEnumerable<AdUser>> GetAllUsersAsync();
    Task<int> GetComputerCountAsync();
    Task<int> GetUserCountAsync();
    DateTime? GetLastComputerSyncDate();
    DateTime? GetLastUserSyncDate();
}
