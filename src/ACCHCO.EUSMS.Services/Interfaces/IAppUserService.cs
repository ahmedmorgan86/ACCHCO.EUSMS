using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Services.Interfaces;

public interface IAppUserService
{
    Task<AppUser?> GetByIdAsync(int id);
    Task<AppUser?> GetByUsernameAsync(string username);
    Task<IEnumerable<AppUser>> GetAllAsync();
    Task<IEnumerable<AppUser>> GetActiveSpecialistsAsync();
    Task<AppUser> CreateAsync(AppUser user, string currentUser);
    Task<AppUser> UpdateAsync(AppUser user, string currentUser);
    Task<bool> DeactivateAsync(int userId, string currentUser);
    Task<bool> AuthenticateAsync(string username, string password);
    Task UpdateLastLoginAsync(int userId);
}
