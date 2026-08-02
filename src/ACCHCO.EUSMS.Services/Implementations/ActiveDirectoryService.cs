using System.DirectoryServices;
using Microsoft.EntityFrameworkCore;
using ACCHCO.EUSMS.Data.Context;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.Services.Implementations;

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly EusmsDbContext _context;

    private static readonly string[] LdapServers = new[]
    {
        "LDAP://172.17.50.11",
        "LDAP://172.17.50.12",
        "LDAP://dct.local"
    };

    private string _workingLdapPath = string.Empty;

    public ActiveDirectoryService(EusmsDbContext context)
    {
        _context = context;
    }

    private string GetWorkingServer()
    {
        if (!string.IsNullOrEmpty(_workingLdapPath))
            return _workingLdapPath;

        FindWorkingServer();
        return _workingLdapPath;
    }

    private void FindWorkingServer()
    {
        foreach (var server in LdapServers)
        {
            try
            {
                using var entry = new DirectoryEntry(server);
                using var searcher = new DirectorySearcher(entry) { PageSize = 1 };
                searcher.Filter = "(objectClass=*)";
                searcher.SizeLimit = 1;
                searcher.SearchScope = SearchScope.Base;
                searcher.ServerTimeLimit = TimeSpan.FromSeconds(5);
                var result = searcher.FindOne();
                if (result != null)
                {
                    _workingLdapPath = server;
                    return;
                }
            }
            catch
            {
                continue;
            }
        }
        _workingLdapPath = LdapServers[0];
    }

    public bool IsAvailable()
    {
        try
        {
            var ldapPath = GetWorkingServer();
            using var entry = new DirectoryEntry(ldapPath);
            using var searcher = new DirectorySearcher(entry) { PageSize = 1 };
            searcher.Filter = "(objectClass=*)";
            searcher.SizeLimit = 1;
            searcher.SearchScope = SearchScope.Base;
            _ = searcher.FindOne();
            return true;
        }
        catch
        {
            _workingLdapPath = string.Empty;
            return false;
        }
    }

    public async Task<IEnumerable<AdComputer>> SyncComputersAsync()
    {
        var ldapPath = GetWorkingServer();
        var synced = new List<AdComputer>();

        await Task.Run(() =>
        {
            using var entry = new DirectoryEntry(ldapPath);
            using var searcher = new DirectorySearcher(entry);
            searcher.Filter = "(&(objectClass=computer))";
            searcher.PropertiesToLoad.AddRange(new[]
            {
                "name", "distinguishedName", "operatingSystem",
                "description", "managedBy",
                "whenCreated", "whenChanged", "objectSID",
                "lastLogon", "userAccountControl"
            });
            searcher.PageSize = 1000;
            searcher.ServerTimeLimit = TimeSpan.FromSeconds(60);

            foreach (SearchResult result in searcher.FindAll())
            {
                var computer = new AdComputer
                {
                    ComputerName = GetProperty(result, "name"),
                    DistinguishedName = GetProperty(result, "distinguishedName"),
                    OperatingSystem = GetProperty(result, "operatingSystem"),
                    Description = GetProperty(result, "description"),
                    ManagedBy = GetProperty(result, "managedBy"),
                    AdSid = GetSid(result),
                    LastSyncDate = DateTime.Now,
                    IsEnabled = IsAdObjectEnabled(result)
                };

                var ouPath = computer.DistinguishedName;
                if (ouPath != null)
                {
                    var parts = ouPath.Split(',');
                    var ouParts = parts.Skip(1).ToArray();
                    computer.OuPath = string.Join(",", ouParts);
                }

                if (result.Properties.Contains("lastLogon") && result.Properties["lastLogon"][0] != null)
                {
                    try
                    {
                        var lastLogonTicks = (long)result.Properties["lastLogon"][0];
                        if (lastLogonTicks > 0)
                            computer.LastLogon = DateTime.FromFileTimeUtc(lastLogonTicks);
                    }
                    catch { }
                }

                if (result.Properties.Contains("whenCreated") && result.Properties["whenCreated"][0] != null)
                    computer.WhenCreated = result.Properties["whenCreated"][0].ToString();

                if (result.Properties.Contains("whenChanged") && result.Properties["whenChanged"][0] != null)
                    computer.WhenChanged = result.Properties["whenChanged"][0].ToString();

                synced.Add(computer);
            }
        });

        if (synced.Count == 0)
            return synced;

        var existing = await _context.AdComputers.ToListAsync();
        var now = DateTime.Now;

        foreach (var computer in synced)
        {
            var found = existing.FirstOrDefault(e =>
                e.ComputerName == computer.ComputerName ||
                (!string.IsNullOrEmpty(e.AdSid) && e.AdSid == computer.AdSid));

            if (found != null)
            {
                found.ComputerName = computer.ComputerName;
                found.DistinguishedName = computer.DistinguishedName;
                found.OperatingSystem = computer.OperatingSystem;
                found.Description = computer.Description;
                found.ManagedBy = computer.ManagedBy;
                found.OuPath = computer.OuPath;
                found.AdSid = computer.AdSid;
                found.IsEnabled = computer.IsEnabled;
                found.LastLogon = computer.LastLogon;
                found.WhenCreated = computer.WhenCreated;
                found.WhenChanged = computer.WhenChanged;
                found.LastSyncDate = now;
                found.ModifiedDate = now;
            }
            else
            {
                computer.CreatedDate = now;
                computer.LastSyncDate = now;
                _context.AdComputers.Add(computer);
            }
        }

        await _context.SaveChangesAsync();
        return synced;
    }

    public async Task<IEnumerable<AdUser>> SyncUsersAsync()
    {
        var ldapPath = GetWorkingServer();
        var synced = new List<AdUser>();

        await Task.Run(() =>
        {
            using var entry = new DirectoryEntry(ldapPath);
            using var searcher = new DirectorySearcher(entry);
            searcher.Filter = "(&(objectClass=user)(objectCategory=person))";
            searcher.PropertiesToLoad.AddRange(new[]
            {
                "sAMAccountName", "displayName", "mail",
                "telephoneNumber", "department", "title",
                "distinguishedName", "manager", "physicalDeliveryOfficeName",
                "whenCreated", "whenChanged", "objectSID",
                "lastLogon", "userAccountControl"
            });
            searcher.PageSize = 1000;
            searcher.ServerTimeLimit = TimeSpan.FromSeconds(60);

            foreach (SearchResult result in searcher.FindAll())
            {
                var user = new AdUser
                {
                    Username = GetProperty(result, "sAMAccountName"),
                    DisplayName = GetProperty(result, "displayName"),
                    Email = GetProperty(result, "mail"),
                    Phone = GetProperty(result, "telephoneNumber"),
                    Department = GetProperty(result, "department"),
                    Title = GetProperty(result, "title"),
                    DistinguishedName = GetProperty(result, "distinguishedName"),
                    Manager = GetProperty(result, "manager"),
                    Office = GetProperty(result, "physicalDeliveryOfficeName"),
                    AdSid = GetSid(result),
                    LastSyncDate = DateTime.Now,
                    IsEnabled = IsAdObjectEnabled(result)
                };

                var ouPath = user.DistinguishedName;
                if (ouPath != null)
                {
                    var parts = ouPath.Split(',');
                    var ouParts = parts.Skip(1).ToArray();
                    user.OuPath = string.Join(",", ouParts);
                }

                if (result.Properties.Contains("whenCreated") && result.Properties["whenCreated"][0] != null)
                {
                    try { user.WhenCreated = result.Properties["whenCreated"][0].ToString(); } catch { }
                }

                if (result.Properties.Contains("whenChanged") && result.Properties["whenChanged"][0] != null)
                {
                    try { user.WhenChanged = result.Properties["whenChanged"][0].ToString(); } catch { }
                }

                if (result.Properties.Contains("lastLogon") && result.Properties["lastLogon"][0] != null)
                {
                    try
                    {
                        var lastLogonTicks = (long)result.Properties["lastLogon"][0];
                        if (lastLogonTicks > 0)
                            user.LastLogon = DateTime.FromFileTimeUtc(lastLogonTicks);
                    }
                    catch { }
                }

                synced.Add(user);
            }
        });

        if (synced.Count == 0)
            return synced;

        var existing = await _context.AdUsers.ToListAsync();
        var now = DateTime.Now;

        foreach (var user in synced)
        {
            var found = existing.FirstOrDefault(e =>
                e.Username == user.Username ||
                (!string.IsNullOrEmpty(e.AdSid) && e.AdSid == user.AdSid));

            if (found != null)
            {
                found.DisplayName = user.DisplayName;
                found.Email = user.Email;
                found.Phone = user.Phone;
                found.Department = user.Department;
                found.Title = user.Title;
                found.DistinguishedName = user.DistinguishedName;
                found.Manager = user.Manager;
                found.Office = user.Office;
                found.AdSid = user.AdSid;
                found.IsEnabled = user.IsEnabled;
                found.LastLogon = user.LastLogon;
                found.WhenCreated = user.WhenCreated;
                found.WhenChanged = user.WhenChanged;
                found.LastSyncDate = now;
                found.ModifiedDate = now;
            }
            else
            {
                user.CreatedDate = now;
                user.LastSyncDate = now;
                _context.AdUsers.Add(user);
            }
        }

        await _context.SaveChangesAsync();
        return synced;
    }

    public async Task<AdComputer?> FindComputerByNameAsync(string computerName)
    {
        return await _context.AdComputers.FirstOrDefaultAsync(c => c.ComputerName == computerName);
    }

    public async Task<AdUser?> FindUserByUsernameAsync(string username)
    {
        return await _context.AdUsers.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<IEnumerable<AdComputer>> GetAllComputersAsync()
    {
        return await _context.AdComputers.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<AdUser>> GetAllUsersAsync()
    {
        return await _context.AdUsers.AsNoTracking().ToListAsync();
    }

    public async Task<int> GetComputerCountAsync()
    {
        return await _context.AdComputers.CountAsync();
    }

    public async Task<int> GetUserCountAsync()
    {
        return await _context.AdUsers.CountAsync();
    }

    public DateTime? GetLastComputerSyncDate()
    {
        var maxDate = _context.AdComputers.Max(c => (DateTime?)c.LastSyncDate);
        return maxDate;
    }

    public DateTime? GetLastUserSyncDate()
    {
        var maxDate = _context.AdUsers.Max(u => (DateTime?)u.LastSyncDate);
        return maxDate;
    }

    private static string GetProperty(SearchResult result, string propertyName)
    {
        if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
            return result.Properties[propertyName][0]?.ToString() ?? string.Empty;
        return string.Empty;
    }

    private static string GetSid(SearchResult result)
    {
        if (result.Properties.Contains("objectSID") && result.Properties["objectSID"].Count > 0)
        {
            var sidBytes = (byte[])result.Properties["objectSID"][0];
            return new System.Security.Principal.SecurityIdentifier(sidBytes, 0).ToString();
        }
        return string.Empty;
    }

    private static bool IsAdObjectEnabled(SearchResult result)
    {
        if (!result.Properties.Contains("userAccountControl") || result.Properties["userAccountControl"].Count == 0)
            return true;
        var flags = (int)result.Properties["userAccountControl"][0];
        return (flags & 0x2) == 0;
    }
}
