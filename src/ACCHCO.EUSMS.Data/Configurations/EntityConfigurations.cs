using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.Data.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Username).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Section).HasMaxLength(100);
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.AdSid).HasMaxLength(200);
        builder.HasIndex(e => e.Username).IsUnique();
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TicketNumber).IsRequired().HasMaxLength(20);
        builder.Property(e => e.RequesterName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.RequesterSection).IsRequired().HasMaxLength(100);
        builder.Property(e => e.RequesterEmail).HasMaxLength(200);
        builder.Property(e => e.RequesterPhone).HasMaxLength(50);
        builder.Property(e => e.Location).HasMaxLength(200);
        builder.Property(e => e.EquipmentName).HasMaxLength(200);
        builder.Property(e => e.AssetTag).HasMaxLength(100);
        builder.Property(e => e.ComputerName).HasMaxLength(100);
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.OperatingSystem).HasMaxLength(100);
        builder.Property(e => e.ProblemDescription).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.ActionsTaken).HasMaxLength(4000);
        builder.Property(e => e.Solution).HasMaxLength(4000);
        builder.Property(e => e.Notes).HasMaxLength(4000);
        builder.HasIndex(e => e.TicketNumber).IsUnique();
        builder.HasIndex(e => e.TicketDate);
        builder.HasIndex(e => e.Status);
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.SupportSpecialist)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(e => e.SupportSpecialistId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("TicketAttachments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FileName).IsRequired().HasMaxLength(500);
        builder.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(500);
        builder.Property(e => e.ContentType).HasMaxLength(200);
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.Ticket)
            .WithMany(t => t.Attachments)
            .HasForeignKey(e => e.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TicketHistoryConfiguration : IEntityTypeConfiguration<TicketHistory>
{
    public void Configure(EntityTypeBuilder<TicketHistory> builder)
    {
        builder.ToTable("TicketHistories");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.OldValue).HasMaxLength(4000);
        builder.Property(e => e.NewValue).HasMaxLength(4000);
        builder.Property(e => e.ChangedBy).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ChangeDescription).HasMaxLength(4000);
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.Ticket)
            .WithMany(t => t.History)
            .HasForeignKey(e => e.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("Equipment");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.AssetTag).HasMaxLength(100);
        builder.Property(e => e.SerialNumber).HasMaxLength(200);
        builder.Property(e => e.Manufacturer).HasMaxLength(200);
        builder.Property(e => e.Model).HasMaxLength(200);
        builder.Property(e => e.ComputerName).HasMaxLength(100);
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.MacAddress).HasMaxLength(50);
        builder.Property(e => e.OperatingSystem).HasMaxLength(100);
        builder.Property(e => e.Location).HasMaxLength(200);
        builder.Property(e => e.Section).HasMaxLength(100);
        builder.Property(e => e.AssignedTo).HasMaxLength(200);
        builder.Property(e => e.Department).HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(4000);
        builder.Property(e => e.BarcodeNumber).HasMaxLength(100);
        builder.Property(e => e.SubnetMask).HasMaxLength(50);
        builder.Property(e => e.DefaultGateway).HasMaxLength(50);
        builder.Property(e => e.DNSServers).HasMaxLength(200);
        builder.HasIndex(e => e.AssetTag);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class ShiftHandoverConfiguration : IEntityTypeConfiguration<ShiftHandover>
{
    public void Configure(EntityTypeBuilder<ShiftHandover> builder)
    {
        builder.ToTable("ShiftHandovers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Summary).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.PendingTasks).HasMaxLength(4000);
        builder.Property(e => e.ImportantNotes).HasMaxLength(4000);
        builder.Property(e => e.EscalationItems).HasMaxLength(4000);
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.FromSpecialist)
            .WithMany(u => u.HandoverFrom)
            .HasForeignKey(e => e.FromSpecialistId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ToSpecialist)
            .WithMany(u => u.HandoverTo)
            .HasForeignKey(e => e.ToSpecialistId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ShiftHandoverTicketConfiguration : IEntityTypeConfiguration<ShiftHandoverTicket>
{
    public void Configure(EntityTypeBuilder<ShiftHandoverTicket> builder)
    {
        builder.ToTable("ShiftHandoverTickets");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.HandoverNotes).HasMaxLength(4000);
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.ShiftHandover)
            .WithMany(h => h.HandoverTickets)
            .HasForeignKey(e => e.ShiftHandoverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Ticket)
            .WithMany()
            .HasForeignKey(e => e.TicketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Action).IsRequired().HasMaxLength(200);
        builder.Property(e => e.EntityName).HasMaxLength(200);
        builder.Property(e => e.EntityId).HasMaxLength(100);
        builder.Property(e => e.UserId).HasMaxLength(200);
        builder.Property(e => e.Username).HasMaxLength(200);
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.MachineName).HasMaxLength(200);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => e.Action);
    }
}

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Key).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Value).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.GroupName).HasMaxLength(200);
        builder.Property(e => e.DataType).HasMaxLength(50);
        builder.HasIndex(e => e.Key).IsUnique();
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class AdComputerConfiguration : IEntityTypeConfiguration<AdComputer>
{
    public void Configure(EntityTypeBuilder<AdComputer> builder)
    {
        builder.ToTable("AdComputers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ComputerName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.DistinguishedName).HasMaxLength(1000);
        builder.Property(e => e.OperatingSystem).HasMaxLength(200);
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ManagedBy).HasMaxLength(200);
        builder.Property(e => e.OuPath).HasMaxLength(1000);
        builder.Property(e => e.AdSid).HasMaxLength(200);
        builder.HasIndex(e => e.ComputerName);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class AdUserConfiguration : IEntityTypeConfiguration<AdUser>
{
    public void Configure(EntityTypeBuilder<AdUser> builder)
    {
        builder.ToTable("AdUsers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Username).IsRequired().HasMaxLength(100);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Department).HasMaxLength(200);
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.DistinguishedName).HasMaxLength(1000);
        builder.Property(e => e.OuPath).HasMaxLength(1000);
        builder.Property(e => e.AdSid).HasMaxLength(200);
        builder.Property(e => e.Manager).HasMaxLength(200);
        builder.Property(e => e.Office).HasMaxLength(200);
        builder.HasIndex(e => e.Username);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class NetworkDeviceConfiguration : IEntityTypeConfiguration<NetworkDevice>
{
    public void Configure(EntityTypeBuilder<NetworkDevice> builder)
    {
        builder.ToTable("NetworkDevices");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Hostname).IsRequired().HasMaxLength(200);
        builder.Property(e => e.IpAddress).IsRequired().HasMaxLength(50);
        builder.Property(e => e.MacAddress).HasMaxLength(50);
        builder.Property(e => e.Manufacturer).HasMaxLength(200);
        builder.Property(e => e.DeviceCategory).HasMaxLength(50);
        builder.Property(e => e.OUPrefix).HasMaxLength(500);
        builder.Property(e => e.Notes).HasMaxLength(4000);
        builder.HasIndex(e => e.IpAddress);
        builder.HasIndex(e => e.Hostname);
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.LinkedEquipment)
            .WithMany()
            .HasForeignKey(e => e.LinkedEquipmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IncidentNumber).HasMaxLength(50);
        builder.HasIndex(x => x.IncidentNumber).IsUnique();
        builder.HasIndex(x => x.IncidentDate);
        builder.HasIndex(x => x.WorkflowStatus);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.Severity);
        builder.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.AssignedSpecialist).WithMany().HasForeignKey(x => x.AssignedSpecialistId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.ConfigurationItem).WithMany().HasForeignKey(x => x.ConfigurationItemId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Problem).WithMany().HasForeignKey(x => x.ProblemId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.KnownError).WithMany().HasForeignKey(x => x.KnownErrorId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Ticket).WithMany().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.SetNull);
        builder.Ignore(x => x.Histories);
        builder.Ignore(x => x.Comments);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class IncidentHistoryConfiguration : IEntityTypeConfiguration<IncidentHistory>
{
    public void Configure(EntityTypeBuilder<IncidentHistory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Incident).WithMany().HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class IncidentCommentConfiguration : IEntityTypeConfiguration<IncidentComment>
{
    public void Configure(EntityTypeBuilder<IncidentComment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Incident).WithMany().HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ConfigurationItemConfiguration : IEntityTypeConfiguration<ConfigurationItem>
{
    public void Configure(EntityTypeBuilder<ConfigurationItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CiNumber).HasMaxLength(50);
        builder.HasIndex(x => x.CiNumber).IsUnique();
        builder.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.LinkedEquipment).WithMany().HasForeignKey(x => x.LinkedEquipmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class CIRelationshipConfiguration : IEntityTypeConfiguration<CIRelationship>
{
    public void Configure(EntityTypeBuilder<CIRelationship> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.SourceCi).WithMany(x => x.ChildRelationships).HasForeignKey(x => x.SourceCiId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TargetCi).WithMany(x => x.ParentRelationships).HasForeignKey(x => x.TargetCiId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ChangeRequestConfiguration : IEntityTypeConfiguration<ChangeRequest>
{
    public void Configure(EntityTypeBuilder<ChangeRequest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ChangeNumber).HasMaxLength(50);
        builder.HasIndex(x => x.ChangeNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.AssignedTo).WithMany().HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ChangeHistoryConfiguration : IEntityTypeConfiguration<ChangeHistory>
{
    public void Configure(EntityTypeBuilder<ChangeHistory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.ChangeRequest).WithMany().HasForeignKey(x => x.ChangeRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ProblemConfiguration : IEntityTypeConfiguration<Problem>
{
    public void Configure(EntityTypeBuilder<Problem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProblemNumber).HasMaxLength(50);
        builder.HasIndex(x => x.ProblemNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ProblemIncidentConfiguration : IEntityTypeConfiguration<ProblemIncident>
{
    public void Configure(EntityTypeBuilder<ProblemIncident> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Problem).WithMany().HasForeignKey(x => x.ProblemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Incident).WithMany().HasForeignKey(x => x.IncidentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class KnownErrorConfiguration : IEntityTypeConfiguration<KnownError>
{
    public void Configure(EntityTypeBuilder<KnownError> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.KnownErrorNumber).HasMaxLength(50);
        builder.HasIndex(x => x.KnownErrorNumber).IsUnique();
        builder.HasOne(x => x.Problem).WithMany().HasForeignKey(x => x.ProblemId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.CreatedUser).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RequestNumber).HasMaxLength(50);
        builder.HasIndex(x => x.RequestNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.AssignedTo).WithMany().HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class EscalationRuleConfiguration : IEntityTypeConfiguration<EscalationRule>
{
    public void Configure(EntityTypeBuilder<EscalationRule> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class SlaDefinitionConfiguration : IEntityTypeConfiguration<SlaDefinition>
{
    public void Configure(EntityTypeBuilder<SlaDefinition> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.IsRead);
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
