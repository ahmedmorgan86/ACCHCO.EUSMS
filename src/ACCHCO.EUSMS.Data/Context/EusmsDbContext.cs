using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Data.Context;

public class EusmsDbContext : DbContext
{
    public EusmsDbContext(DbContextOptions<EusmsDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<ShiftHandover> ShiftHandovers => Set<ShiftHandover>();
    public DbSet<ShiftHandoverTicket> ShiftHandoverTickets => Set<ShiftHandoverTicket>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<AdComputer> AdComputers => Set<AdComputer>();
    public DbSet<AdUser> AdUsers => Set<AdUser>();
    public DbSet<NetworkDevice> NetworkDevices => Set<NetworkDevice>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentHistory> IncidentHistories => Set<IncidentHistory>();
    public DbSet<IncidentComment> IncidentComments => Set<IncidentComment>();
    public DbSet<ConfigurationItem> ConfigurationItems => Set<ConfigurationItem>();
    public DbSet<CIRelationship> CIRelationships => Set<CIRelationship>();
    public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();
    public DbSet<ChangeHistory> ChangeHistories => Set<ChangeHistory>();
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<ProblemIncident> ProblemIncidents => Set<ProblemIncident>();
    public DbSet<KnownError> KnownErrors => Set<KnownError>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<EscalationRule> EscalationRules => Set<EscalationRule>();
    public DbSet<SlaDefinition> SlaDefinitions => Set<SlaDefinition>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DeviceReplacement> DeviceReplacements => Set<DeviceReplacement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EusmsDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return SaveWithRetryAsync().GetAwaiter().GetResult();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return await SaveWithRetryAsync(cancellationToken);
    }

    private const int MaxSaveRetries = 6;

    private async Task<int> SaveWithRetryAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var result = await base.SaveChangesAsync(cancellationToken);
                SyncStampStore.Increment(Database.GetDbConnection().ConnectionString);
                return result;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode is 5 or 6 && attempt < MaxSaveRetries)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(150 * (attempt + 1)), cancellationToken);
            }
        }
    }

    private void ApplyAuditInfo()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = DateTime.Now;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedDate = DateTime.Now;
                    break;
            }
        }
    }
}
