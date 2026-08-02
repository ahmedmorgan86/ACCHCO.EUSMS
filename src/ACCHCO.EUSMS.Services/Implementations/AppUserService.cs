using Microsoft.EntityFrameworkCore;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;
using ACCHCO.EUSMS.Services.Helpers;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.Services.Implementations;

public class AppUserService : IAppUserService
{
    private readonly IUserRepository _repo;
    private readonly IAuditService _auditService;

    public AppUserService(IUserRepository repo, IAuditService auditService)
    {
        _repo = repo;
        _auditService = auditService;
    }

    public async Task<AppUser?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);

    public async Task<AppUser?> GetByUsernameAsync(string username) =>
        await _repo.GetByUsernameAsync(username);

    public async Task<IEnumerable<AppUser>> GetAllAsync() =>
        await _repo.Query().Where(u => !u.IsDeleted).ToListAsync();

    public async Task<IEnumerable<AppUser>> GetActiveSpecialistsAsync()
    {
        return await _repo.Query()
            .Where(u => u.IsActive && !u.IsDeleted && u.Role == UserRole.SupportSpecialist)
            .ToListAsync();
    }

    public async Task<AppUser> CreateAsync(AppUser user, string currentUser)
    {
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            user.PasswordHash = PasswordHasher.Hash("P@ssw0rd!");
            user.LastPasswordChangeDate = DateTime.Now;
        }

        user.CreatedBy = currentUser;
        var created = await _repo.AddAsync(user);
        await _auditService.LogAsync("Create", "User", created.Id.ToString(),
            newValues: $"User '{created.Username}' created with role {created.Role}",
            userId: currentUser, username: currentUser);
        return created;
    }

    public async Task<AppUser> UpdateAsync(AppUser user, string currentUser)
    {
        user.ModifiedBy = currentUser;
        await _repo.UpdateAsync(user);
        await _auditService.LogAsync("Update", "User", user.Id.ToString(),
            newValues: $"User '{user.Username}' updated",
            userId: currentUser, username: currentUser);
        return user;
    }

    public async Task<bool> DeactivateAsync(int userId, string currentUser)
    {
        var user = await _repo.GetByIdAsync(userId);
        if (user == null) return false;

        user.IsActive = false;
        user.ModifiedBy = currentUser;
        await _repo.UpdateAsync(user);
        await _auditService.LogAsync("Deactivate", "User", userId.ToString(),
            newValues: $"User '{user.Username}' deactivated",
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        var user = await _repo.GetByUsernameAsync(username);
        if (user == null || !user.IsActive || user.IsDeleted)
        {
            return false;
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash) || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.Now.AddMinutes(15);
                user.FailedLoginAttempts = 0;
            }

            await _repo.UpdateAsync(user);
            return false;
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginDate = DateTime.Now;
        await _repo.UpdateAsync(user);

        await _auditService.LogAsync("Authenticate", "User", user.Id.ToString(),
            newValues: $"User '{user.Username}' authenticated successfully",
            userId: user.Id.ToString(), username: user.Username);

        return true;
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _repo.GetByIdAsync(userId);
        if (user != null)
        {
            user.LastLoginDate = DateTime.Now;
            user.FailedLoginAttempts = 0;
            await _repo.UpdateAsync(user);
        }
    }
}
