using Microsoft.EntityFrameworkCore;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.Services.Implementations;

public class IncidentService : IIncidentService
{
    private readonly IRepository<Incident> _incidentRepo;
    private readonly IRepository<IncidentComment> _commentRepo;
    private readonly IRepository<IncidentHistory> _historyRepo;
    private readonly IAuditService _auditService;
    private readonly ISlaService _slaService;
    private readonly INotificationService _notificationService;

    public IncidentService(IRepository<Incident> incidentRepo, IRepository<IncidentComment> commentRepo,
        IRepository<IncidentHistory> historyRepo, IAuditService auditService,
        ISlaService slaService, INotificationService notificationService)
    {
        _incidentRepo = incidentRepo;
        _commentRepo = commentRepo;
        _historyRepo = historyRepo;
        _auditService = auditService;
        _slaService = slaService;
        _notificationService = notificationService;
    }

    public async Task<Incident?> GetByIdAsync(int id) => await _incidentRepo.GetByIdAsync(id);

    public async Task<Incident?> GetByNumberAsync(string incidentNumber) =>
        await _incidentRepo.FirstOrDefaultAsync(i => i.IncidentNumber == incidentNumber);

    public async Task<Incident?> GetWithDetailsAsync(int id)
    {
        return await _incidentRepo.Query()
            .Include(i => i.Requester)
            .Include(i => i.AssignedSpecialist)
            .Include(i => i.ConfigurationItem)
            .Include(i => i.Problem)
            .Include(i => i.KnownError)
            .Include(i => i.Comments.OrderByDescending(c => c.CommentDate))
            .Include(i => i.Histories.OrderByDescending(h => h.ChangedDate))
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
    }

    public async Task<IEnumerable<Incident>> GetAllAsync() =>
        await _incidentRepo.Query().Where(i => !i.IsDeleted).OrderByDescending(i => i.IncidentDate).ToListAsync();

    public async Task<IEnumerable<Incident>> SearchAsync(string query)
    {
        return await _incidentRepo.Query()
            .Where(i => !i.IsDeleted &&
                (i.IncidentNumber.Contains(query) ||
                 i.RequesterName.Contains(query) ||
                 i.ProblemDescription.Contains(query) ||
                 (i.EquipmentName != null && i.EquipmentName.Contains(query)) ||
                 (i.Category != null && i.Category.Contains(query)) ||
                 (i.Location != null && i.Location.Contains(query))))
            .OrderByDescending(i => i.IncidentDate)
            .ToListAsync();
    }

    public async Task<Incident> CreateAsync(Incident incident, string currentUser)
    {
        incident.IncidentNumber = await GenerateNextNumberAsync();
        incident.IncidentDate = incident.IncidentDate == default ? DateTime.Now : incident.IncidentDate;
        incident.IncidentTime = incident.IncidentTime == default ? DateTime.Now.TimeOfDay : incident.IncidentTime;
        incident.WorkflowStatus = WorkflowStatus.New;
        incident.CreatedBy = currentUser;
        var created = await _incidentRepo.AddAsync(incident);

        await _auditService.LogAsync("Create", "Incident", created.Id.ToString(),
            newValues: $"Incident {created.IncidentNumber} created",
            userId: currentUser, username: currentUser);

        await _notificationService.SendAssignmentNotificationAsync(
            created.AssignedSpecialistId ?? 0, currentUser, "Incident", created.Id, created.IncidentNumber);

        return created;
    }

    public async Task<Incident> UpdateAsync(Incident incident, string currentUser)
    {
        var existing = await _incidentRepo.GetByIdAsync(incident.Id);
        if (existing == null)
            throw new InvalidOperationException($"Incident {incident.Id} not found.");

        var changes = GetChanges(existing, incident);
        incident.ModifiedBy = currentUser;
        await _incidentRepo.UpdateAsync(incident);

        foreach (var change in changes)
        {
            await _auditService.LogAsync("Update", "Incident", incident.Id.ToString(),
                oldValues: $"{change.FieldName}: {change.OldValue}",
                newValues: $"{change.FieldName}: {change.NewValue}",
                userId: currentUser, username: currentUser);

            await _historyRepo.AddAsync(new IncidentHistory
            {
                IncidentId = incident.Id,
                FieldName = change.FieldName,
                OldValue = change.OldValue,
                NewValue = change.NewValue,
                ChangedBy = currentUser,
                ChangedDate = DateTime.Now,
                ChangeDescription = $"{change.FieldName} changed from {change.OldValue} to {change.NewValue}"
            });
        }

        return incident;
    }

    public async Task<bool> DeleteAsync(int incidentId, string currentUser)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);
        if (incident == null) return false;

        await _incidentRepo.SoftDeleteAsync(incident);
        await _auditService.LogAsync("Delete", "Incident", incidentId.ToString(),
            oldValues: $"Incident {incident.IncidentNumber} deleted",
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<bool> AssignAsync(int incidentId, int specialistId, string currentUser)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);
        if (incident == null) return false;

        var oldSpecialistId = incident.AssignedSpecialistId;
        incident.AssignedSpecialistId = specialistId;
        incident.WorkflowStatus = WorkflowStatus.Assigned;
        incident.ModifiedBy = currentUser;
        await _incidentRepo.UpdateAsync(incident);

        await _auditService.LogAsync("Assign", "Incident", incidentId.ToString(),
            oldValues: $"AssignedSpecialistId: {oldSpecialistId}",
            newValues: $"AssignedSpecialistId: {specialistId}",
            userId: currentUser, username: currentUser);

        await _historyRepo.AddAsync(new IncidentHistory
        {
            IncidentId = incidentId,
            FieldName = "AssignedSpecialist",
            OldValue = oldSpecialistId?.ToString(),
            NewValue = specialistId.ToString(),
            ChangedBy = currentUser,
            ChangedDate = DateTime.Now,
            ChangeDescription = "Incident assigned"
        });

        await _notificationService.SendAssignmentNotificationAsync(
            specialistId, currentUser, "Incident", incidentId, incident.IncidentNumber);

        return true;
    }

    public async Task<bool> EscalateAsync(int incidentId, EscalationLevel level, string currentUser)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);
        if (incident == null) return false;

        var oldLevel = incident.EscalationLevel;
        incident.EscalationLevel = level;
        incident.LastEscalationCheck = DateTime.Now;
        incident.ModifiedBy = currentUser;
        await _incidentRepo.UpdateAsync(incident);

        await _auditService.LogAsync("Escalate", "Incident", incidentId.ToString(),
            oldValues: $"EscalationLevel: {oldLevel}",
            newValues: $"EscalationLevel: {level}",
            userId: currentUser, username: currentUser);

        await _historyRepo.AddAsync(new IncidentHistory
        {
            IncidentId = incidentId,
            FieldName = "EscalationLevel",
            OldValue = oldLevel.ToString(),
            NewValue = level.ToString(),
            ChangedBy = currentUser,
            ChangedDate = DateTime.Now,
            ChangeDescription = $"Incident escalated to {level}"
        });

        await _notificationService.SendEscalationNotificationAsync(
            incident.AssignedSpecialistId ?? 0, currentUser, "Incident", incidentId, incident.IncidentNumber, level);

        return true;
    }

    public async Task<bool> ResolveAsync(int incidentId, string resolutionNotes, string currentUser)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);
        if (incident == null) return false;

        incident.WorkflowStatus = WorkflowStatus.Resolved;
        incident.ResolutionNotes = resolutionNotes;
        incident.ResolvedDate = DateTime.Now;
        incident.ModifiedBy = currentUser;

        if (incident.AcknowledgedDate.HasValue)
            incident.ResolutionTimeMinutes = (DateTime.Now - incident.AcknowledgedDate.Value).TotalMinutes;

        await _incidentRepo.UpdateAsync(incident);

        await _auditService.LogAsync("Resolve", "Incident", incidentId.ToString(),
            newValues: $"Incident resolved: {resolutionNotes}",
            userId: currentUser, username: currentUser);

        await _historyRepo.AddAsync(new IncidentHistory
        {
            IncidentId = incidentId,
            FieldName = "WorkflowStatus",
            OldValue = WorkflowStatus.InProgress.ToString(),
            NewValue = WorkflowStatus.Resolved.ToString(),
            ChangedBy = currentUser,
            ChangedDate = DateTime.Now,
            ChangeDescription = "Incident resolved"
        });

        return true;
    }

    public async Task<bool> CloseAsync(int incidentId, string closureNotes, string closureCode, bool customerSatisfied, string currentUser)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);
        if (incident == null) return false;

        incident.WorkflowStatus = WorkflowStatus.Closed;
        incident.ClosureNotes = closureNotes;
        incident.ClosureCode = closureCode;
        incident.CustomerSatisfied = customerSatisfied;
        incident.ClosedDate = DateTime.Now;
        incident.ModifiedBy = currentUser;

        if (incident.AcknowledgedDate.HasValue)
            incident.ResolutionTimeMinutes = (incident.ClosedDate.Value - incident.AcknowledgedDate.Value).TotalMinutes;

        await _incidentRepo.UpdateAsync(incident);

        await _auditService.LogAsync("Close", "Incident", incidentId.ToString(),
            newValues: $"Incident closed. Code: {closureCode}",
            userId: currentUser, username: currentUser);

        await _historyRepo.AddAsync(new IncidentHistory
        {
            IncidentId = incidentId,
            FieldName = "WorkflowStatus",
            OldValue = WorkflowStatus.Resolved.ToString(),
            NewValue = WorkflowStatus.Closed.ToString(),
            ChangedBy = currentUser,
            ChangedDate = DateTime.Now,
            ChangeDescription = "Incident closed"
        });

        return true;
    }

    public async Task<bool> ReopenAsync(int incidentId, string currentUser)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);
        if (incident == null) return false;

        incident.WorkflowStatus = WorkflowStatus.New;
        incident.ResolvedDate = null;
        incident.ClosedDate = null;
        incident.ClosureNotes = null;
        incident.ClosureCode = null;
        incident.ModifiedBy = currentUser;
        await _incidentRepo.UpdateAsync(incident);

        await _auditService.LogAsync("Reopen", "Incident", incidentId.ToString(),
            newValues: "Incident reopened",
            userId: currentUser, username: currentUser);

        await _historyRepo.AddAsync(new IncidentHistory
        {
            IncidentId = incidentId,
            FieldName = "WorkflowStatus",
            OldValue = WorkflowStatus.Closed.ToString(),
            NewValue = WorkflowStatus.New.ToString(),
            ChangedBy = currentUser,
            ChangedDate = DateTime.Now,
            ChangeDescription = "Incident reopened"
        });

        return true;
    }

    public async Task<IncidentComment> AddCommentAsync(int incidentId, string comment, int? authorId, string authorName, bool isInternal)
    {
        var incidentComment = new IncidentComment
        {
            IncidentId = incidentId,
            Comment = comment,
            AuthorId = authorId,
            AuthorName = authorName,
            IsInternal = isInternal,
            CommentDate = DateTime.Now
        };

        return await _commentRepo.AddAsync(incidentComment);
    }

    public async Task<IEnumerable<IncidentComment>> GetCommentsAsync(int incidentId)
    {
        return await _commentRepo.Query()
            .Where(c => c.IncidentId == incidentId)
            .OrderByDescending(c => c.CommentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<IncidentHistory>> GetHistoryAsync(int incidentId)
    {
        return await _historyRepo.Query()
            .Where(h => h.IncidentId == incidentId)
            .OrderByDescending(h => h.ChangedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Incident>> GetFilteredAsync(
        DateTime? from, DateTime? to, Shift? shift, int? assignedSpecialistId,
        string? section, EquipmentType? equipmentType, Priority? priority,
        IncidentSeverity? severity, IncidentStatus? status,
        EscalationLevel? escalationLevel, int? requesterId)
    {
        var query = _incidentRepo.Query().Where(i => !i.IsDeleted);

        if (from.HasValue)
            query = query.Where(i => i.IncidentDate >= from.Value);
        if (to.HasValue)
            query = query.Where(i => i.IncidentDate <= to.Value);
        if (shift.HasValue)
            query = query.Where(i => i.Shift == shift.Value);
        if (assignedSpecialistId.HasValue)
            query = query.Where(i => i.AssignedSpecialistId == assignedSpecialistId.Value);
        if (!string.IsNullOrWhiteSpace(section))
            query = query.Where(i => i.RequesterSection == section);
        if (equipmentType.HasValue)
            query = query.Where(i => i.EquipmentType == equipmentType.Value);
        if (priority.HasValue)
            query = query.Where(i => i.Priority == priority.Value);
        if (severity.HasValue)
            query = query.Where(i => i.Severity == severity.Value);
        if (escalationLevel.HasValue)
            query = query.Where(i => i.EscalationLevel == escalationLevel.Value);
        if (requesterId.HasValue)
            query = query.Where(i => i.RequesterId == requesterId.Value);

        if (status.HasValue)
        {
            query = status.Value switch
            {
                IncidentStatus.New => query.Where(i => i.WorkflowStatus == WorkflowStatus.New),
                IncidentStatus.Assigned => query.Where(i => i.WorkflowStatus == WorkflowStatus.Assigned),
                IncidentStatus.InProgress => query.Where(i => i.WorkflowStatus == WorkflowStatus.InProgress),
                IncidentStatus.Resolved => query.Where(i => i.WorkflowStatus == WorkflowStatus.Resolved),
                IncidentStatus.Closed => query.Where(i => i.WorkflowStatus == WorkflowStatus.Closed),
                IncidentStatus.Reopened => query.Where(i => i.ResolvedDate == null && i.AcknowledgedDate != null),
                _ => query
            };
        }

        return await query.OrderByDescending(i => i.IncidentDate).ToListAsync();
    }

    public async Task<DashboardKpiDto> GetDashboardKpisAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var allIncidents = await _incidentRepo.Query().Where(i => !i.IsDeleted).ToListAsync();

        var totalOpen = allIncidents.Count(i =>
            i.WorkflowStatus != WorkflowStatus.Resolved &&
            i.WorkflowStatus != WorkflowStatus.Closed &&
            i.WorkflowStatus != WorkflowStatus.Cancelled);
        var totalResolved = allIncidents.Count(i => i.WorkflowStatus == WorkflowStatus.Resolved || i.WorkflowStatus == WorkflowStatus.Closed);
        var totalCritical = allIncidents.Count(i => i.Severity == IncidentSeverity.Critical &&
            i.WorkflowStatus != WorkflowStatus.Resolved && i.WorkflowStatus != WorkflowStatus.Closed);
        var totalEscalated = allIncidents.Count(i => i.EscalationLevel > EscalationLevel.None);
        var createdThisMonth = allIncidents.Count(i => i.CreatedDate >= monthStart);
        var resolvedThisMonth = allIncidents.Count(i =>
            (i.WorkflowStatus == WorkflowStatus.Resolved || i.WorkflowStatus == WorkflowStatus.Closed) &&
            i.ResolvedDate.HasValue && i.ResolvedDate.Value >= monthStart);
        var avgResolutionTime = allIncidents
            .Where(i => i.ResolutionTimeMinutes.HasValue && i.ResolutionTimeMinutes > 0)
            .Select(i => i.ResolutionTimeMinutes!.Value);
        var mttr = avgResolutionTime.Any() ? avgResolutionTime.Average() : 0;

        return new DashboardKpiDto
        {
            TotalOpen = totalOpen,
            TotalResolved = totalResolved,
            TotalCritical = totalCritical,
            TotalEscalated = totalEscalated,
            CreatedThisMonth = createdThisMonth,
            ResolvedThisMonth = resolvedThisMonth,
            AverageResolutionTimeMinutes = Math.Round(mttr, 2)
        };
    }

    public async Task<bool> CheckDuplicateAsync(int? equipmentTypeId, string? equipmentName, string? description, int? excludeIncidentId)
    {
        var query = _incidentRepo.Query().Where(i => !i.IsDeleted &&
            i.WorkflowStatus != WorkflowStatus.Resolved &&
            i.WorkflowStatus != WorkflowStatus.Closed);

        if (excludeIncidentId.HasValue)
            query = query.Where(i => i.Id != excludeIncidentId.Value);

        if (equipmentTypeId.HasValue)
            query = query.Where(i => i.EquipmentType == (EquipmentType)equipmentTypeId.Value);

        if (!string.IsNullOrWhiteSpace(equipmentName))
            query = query.Where(i => i.EquipmentName != null && i.EquipmentName == equipmentName);

        if (!string.IsNullOrWhiteSpace(description))
        {
            var words = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(i => words.Any(w => i.ProblemDescription.Contains(w)));
        }

        return await query.AnyAsync();
    }

    public async Task AutoEscalateAsync()
    {
        var openIncidents = await _incidentRepo.Query()
            .Where(i => !i.IsDeleted &&
                i.WorkflowStatus != WorkflowStatus.Resolved &&
                i.WorkflowStatus != WorkflowStatus.Closed &&
                i.WorkflowStatus != WorkflowStatus.Cancelled)
            .ToListAsync();

        foreach (var incident in openIncidents)
        {
            var slaStatus = await _slaService.GetSlaStatusAsync(incident.Id);
            if (slaStatus == null) continue;

            var isBreached = slaStatus.IsBreached;
            var remainingMinutes = slaStatus.RemainingMinutes;

            if (isBreached && incident.EscalationLevel < EscalationLevel.Management)
            {
                var newLevel = incident.EscalationLevel switch
                {
                    EscalationLevel.None => EscalationLevel.Level1,
                    EscalationLevel.Level1 => EscalationLevel.Level2,
                    EscalationLevel.Level2 => EscalationLevel.Level3,
                    EscalationLevel.Level3 => EscalationLevel.Management,
                    _ => incident.EscalationLevel
                };

                incident.EscalationLevel = newLevel;
                incident.LastEscalationCheck = DateTime.Now;
                await _incidentRepo.UpdateAsync(incident);

                await _auditService.LogAsync("AutoEscalate", "Incident", incident.Id.ToString(),
                    newValues: $"Auto-escalated to {newLevel} due to SLA breach",
                    userId: "System", username: "System");

                await _notificationService.SendEscalationNotificationAsync(
                    incident.AssignedSpecialistId ?? 0, "System", "Incident",
                    incident.Id, incident.IncidentNumber, newLevel);
            }
            else if (!isBreached && remainingMinutes > 0 && remainingMinutes <= 30 && incident.EscalationLevel == EscalationLevel.None)
            {
                await _notificationService.SendSlaWarningAsync(
                    incident.AssignedSpecialistId ?? 0, "System", "Incident",
                    incident.Id, incident.IncidentNumber, remainingMinutes);
            }
        }
    }

    private async Task<string> GenerateNextNumberAsync()
    {
        return await _incidentRepo.GenerateSequentialNumberAsync("Incident", "INC-", nameof(Incident.IncidentNumber));
    }

    private List<(string FieldName, string? OldValue, string? NewValue)> GetChanges(Incident old, Incident updated)
    {
        var changes = new List<(string, string?, string?)>();

        if (old.Severity != updated.Severity) changes.Add(("Severity", old.Severity.ToString(), updated.Severity.ToString()));
        if (old.Impact != updated.Impact) changes.Add(("Impact", old.Impact.ToString(), updated.Impact.ToString()));
        if (old.Urgency != updated.Urgency) changes.Add(("Urgency", old.Urgency.ToString(), updated.Urgency.ToString()));
        if (old.Priority != updated.Priority) changes.Add(("Priority", old.Priority.ToString(), updated.Priority.ToString()));
        if (old.RequesterName != updated.RequesterName) changes.Add(("RequesterName", old.RequesterName, updated.RequesterName));
        if (old.RequesterSection != updated.RequesterSection) changes.Add(("RequesterSection", old.RequesterSection, updated.RequesterSection));
        if (old.ProblemDescription != updated.ProblemDescription) changes.Add(("ProblemDescription", old.ProblemDescription, updated.ProblemDescription));
        if (old.ImpactDescription != updated.ImpactDescription) changes.Add(("ImpactDescription", old.ImpactDescription, updated.ImpactDescription));
        if (old.Workaround != updated.Workaround) changes.Add(("Workaround", old.Workaround, updated.Workaround));
        if (old.ResolutionNotes != updated.ResolutionNotes) changes.Add(("ResolutionNotes", old.ResolutionNotes, updated.ResolutionNotes));
        if (old.Category != updated.Category) changes.Add(("Category", old.Category, updated.Category));
        if (old.EquipmentType != updated.EquipmentType) changes.Add(("EquipmentType", old.EquipmentType?.ToString(), updated.EquipmentType?.ToString()));
        if (old.EquipmentName != updated.EquipmentName) changes.Add(("EquipmentName", old.EquipmentName, updated.EquipmentName));
        if (old.Location != updated.Location) changes.Add(("Location", old.Location, updated.Location));
        if (old.ComputerName != updated.ComputerName) changes.Add(("ComputerName", old.ComputerName, updated.ComputerName));
        if (old.IpAddress != updated.IpAddress) changes.Add(("IpAddress", old.IpAddress, updated.IpAddress));
        if (old.AssignedSpecialistId != updated.AssignedSpecialistId) changes.Add(("AssignedSpecialistId", old.AssignedSpecialistId?.ToString(), updated.AssignedSpecialistId?.ToString()));
        if (old.WorkflowStatus != updated.WorkflowStatus) changes.Add(("WorkflowStatus", old.WorkflowStatus.ToString(), updated.WorkflowStatus.ToString()));
        if (old.EscalationLevel != updated.EscalationLevel) changes.Add(("EscalationLevel", old.EscalationLevel.ToString(), updated.EscalationLevel.ToString()));
        if (old.Source != updated.Source) changes.Add(("Source", old.Source.ToString(), updated.Source.ToString()));
        if (old.ClosureCode != updated.ClosureCode) changes.Add(("ClosureCode", old.ClosureCode, updated.ClosureCode));
        if (old.ClosureNotes != updated.ClosureNotes) changes.Add(("ClosureNotes", old.ClosureNotes, updated.ClosureNotes));

        return changes;
    }
}

public class ChangeRequestService : IChangeRequestService
{
    private readonly IRepository<ChangeRequest> _changeRepo;
    private readonly IRepository<ChangeHistory> _historyRepo;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public ChangeRequestService(IRepository<ChangeRequest> changeRepo, IRepository<ChangeHistory> historyRepo,
        IAuditService auditService, INotificationService notificationService)
    {
        _changeRepo = changeRepo;
        _historyRepo = historyRepo;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<ChangeRequest?> GetByIdAsync(int id) => await _changeRepo.GetByIdAsync(id);

    public async Task<ChangeRequest?> GetByNumberAsync(string changeNumber) =>
        await _changeRepo.FirstOrDefaultAsync(c => c.ChangeNumber == changeNumber);

    public async Task<IEnumerable<ChangeRequest>> GetAllAsync() =>
        await _changeRepo.Query().Where(c => !c.IsDeleted).OrderByDescending(c => c.CreatedDate).ToListAsync();

    public async Task<ChangeRequest> CreateAsync(ChangeRequest changeRequest, string currentUser)
    {
        changeRequest.ChangeNumber = await GenerateNextNumberAsync();
        changeRequest.Status = ChangeStatus.Draft;
        changeRequest.ApprovalStatus = ChangeApprovalStatus.Pending;
        changeRequest.CreatedBy = currentUser;
        var created = await _changeRepo.AddAsync(changeRequest);

        await _auditService.LogAsync("Create", "ChangeRequest", created.Id.ToString(),
            newValues: $"Change {created.ChangeNumber} created",
            userId: currentUser, username: currentUser);

        return created;
    }

    public async Task<ChangeRequest> UpdateAsync(ChangeRequest changeRequest, string currentUser)
    {
        var existing = await _changeRepo.GetByIdAsync(changeRequest.Id);
        if (existing == null)
            throw new InvalidOperationException($"Change request {changeRequest.Id} not found.");

        var changes = GetChanges(existing, changeRequest);
        changeRequest.ModifiedBy = currentUser;
        await _changeRepo.UpdateAsync(changeRequest);

        foreach (var change in changes)
        {
            await _auditService.LogAsync("Update", "ChangeRequest", changeRequest.Id.ToString(),
                oldValues: $"{change.FieldName}: {change.OldValue}",
                newValues: $"{change.FieldName}: {change.NewValue}",
                userId: currentUser, username: currentUser);

            await _historyRepo.AddAsync(new ChangeHistory
            {
                ChangeRequestId = changeRequest.Id,
                FieldName = change.FieldName,
                OldValue = change.OldValue,
                NewValue = change.NewValue,
                ChangedBy = currentUser,
                ChangedDate = DateTime.Now,
                ChangeDescription = $"{change.FieldName} changed from {change.OldValue} to {change.NewValue}"
            });
        }

        return changeRequest;
    }

    public async Task<bool> DeleteAsync(int changeRequestId, string currentUser)
    {
        var changeRequest = await _changeRepo.GetByIdAsync(changeRequestId);
        if (changeRequest == null) return false;

        await _changeRepo.SoftDeleteAsync(changeRequest);
        await _auditService.LogAsync("Delete", "ChangeRequest", changeRequestId.ToString(),
            oldValues: $"Change {changeRequest.ChangeNumber} deleted",
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<bool> SubmitAsync(int changeRequestId, string currentUser)
    {
        var changeRequest = await _changeRepo.GetByIdAsync(changeRequestId);
        if (changeRequest == null || changeRequest.Status != ChangeStatus.Draft) return false;

        changeRequest.Status = ChangeStatus.Submitted;
        changeRequest.ModifiedBy = currentUser;
        await _changeRepo.UpdateAsync(changeRequest);

        await _auditService.LogAsync("Submit", "ChangeRequest", changeRequestId.ToString(),
            newValues: $"Change {changeRequest.ChangeNumber} submitted for approval",
            userId: currentUser, username: currentUser);

        await _historyRepo.AddAsync(new ChangeHistory
        {
            ChangeRequestId = changeRequestId,
            FieldName = "Status",
            OldValue = ChangeStatus.Draft.ToString(),
            NewValue = ChangeStatus.Submitted.ToString(),
            ChangedBy = currentUser,
            ChangedDate = DateTime.Now,
            ChangeDescription = "Change request submitted"
        });

        return true;
    }

    public async Task<bool> ApproveAsync(int changeRequestId, string approverName, string? notes, string currentUser)
    {
        var changeRequest = await _changeRepo.GetByIdAsync(changeRequestId);
        if (changeRequest == null || changeRequest.Status != ChangeStatus.Submitted) return false;

        changeRequest.Status = ChangeStatus.Approved;
        changeRequest.ApprovalStatus = ChangeApprovalStatus.Approved;
        changeRequest.ApproverName = approverName;
        changeRequest.ApprovalNotes = notes;
        changeRequest.ApprovalDate = DateTime.Now;
        changeRequest.ModifiedBy = currentUser;
        await _changeRepo.UpdateAsync(changeRequest);

        await _auditService.LogAsync("Approve", "ChangeRequest", changeRequestId.ToString(),
            newValues: $"Change {changeRequest.ChangeNumber} approved by {approverName}",
            userId: currentUser, username: currentUser);

        await _historyRepo.AddAsync(new ChangeHistory
        {
            ChangeRequestId = changeRequestId,
            FieldName = "ApprovalStatus",
            OldValue = ChangeApprovalStatus.Pending.ToString(),
            NewValue = ChangeApprovalStatus.Approved.ToString(),
            ChangedBy = currentUser,
            ChangedDate = DateTime.Now,
            ChangeDescription = $"Change approved by {approverName}"
        });

        return true;
    }

    public async Task<bool> RejectAsync(int changeRequestId, string approverName, string? notes, string currentUser)
    {
        var changeRequest = await _changeRepo.GetByIdAsync(changeRequestId);
        if (changeRequest == null || changeRequest.Status != ChangeStatus.Submitted) return false;

        changeRequest.Status = ChangeStatus.Cancelled;
        changeRequest.ApprovalStatus = ChangeApprovalStatus.Rejected;
        changeRequest.ApproverName = approverName;
        changeRequest.ApprovalNotes = notes;
        changeRequest.ApprovalDate = DateTime.Now;
        changeRequest.ModifiedBy = currentUser;
        await _changeRepo.UpdateAsync(changeRequest);

        await _auditService.LogAsync("Reject", "ChangeRequest", changeRequestId.ToString(),
            newValues: $"Change {changeRequest.ChangeNumber} rejected by {approverName}",
            userId: currentUser, username: currentUser);

        await _historyRepo.AddAsync(new ChangeHistory
        {
            ChangeRequestId = changeRequestId,
            FieldName = "ApprovalStatus",
            OldValue = ChangeApprovalStatus.Pending.ToString(),
            NewValue = ChangeApprovalStatus.Rejected.ToString(),
            ChangedBy = currentUser,
            ChangedDate = DateTime.Now,
            ChangeDescription = $"Change rejected by {approverName}"
        });

        return true;
    }

    public async Task<bool> StartImplementationAsync(int changeRequestId, string currentUser)
    {
        var changeRequest = await _changeRepo.GetByIdAsync(changeRequestId);
        if (changeRequest == null || changeRequest.Status != ChangeStatus.Approved) return false;

        changeRequest.Status = ChangeStatus.Implementing;
        changeRequest.ImplementationDate = DateTime.Now;
        changeRequest.ModifiedBy = currentUser;
        await _changeRepo.UpdateAsync(changeRequest);

        await _auditService.LogAsync("StartImplementation", "ChangeRequest", changeRequestId.ToString(),
            newValues: $"Change {changeRequest.ChangeNumber} implementation started",
            userId: currentUser, username: currentUser);

        await _historyRepo.AddAsync(new ChangeHistory
        {
            ChangeRequestId = changeRequestId,
            FieldName = "Status",
            OldValue = ChangeStatus.Approved.ToString(),
            NewValue = ChangeStatus.Implementing.ToString(),
            ChangedBy = currentUser,
            ChangedDate = DateTime.Now,
            ChangeDescription = "Implementation started"
        });

        return true;
    }

    public async Task<bool> CompleteAsync(int changeRequestId, string? implementationNotes, string? testingResult, string currentUser)
    {
        var changeRequest = await _changeRepo.GetByIdAsync(changeRequestId);
        if (changeRequest == null || changeRequest.Status != ChangeStatus.Implementing) return false;

        changeRequest.Status = ChangeStatus.Completed;
        changeRequest.CompletionDate = DateTime.Now;
        changeRequest.ImplementationNotes = implementationNotes;
        changeRequest.TestingResult = testingResult;
        changeRequest.ModifiedBy = currentUser;
        await _changeRepo.UpdateAsync(changeRequest);

        await _auditService.LogAsync("Complete", "ChangeRequest", changeRequestId.ToString(),
            newValues: $"Change {changeRequest.ChangeNumber} completed",
            userId: currentUser, username: currentUser);

        await _historyRepo.AddAsync(new ChangeHistory
        {
            ChangeRequestId = changeRequestId,
            FieldName = "Status",
            OldValue = ChangeStatus.Implementing.ToString(),
            NewValue = ChangeStatus.Completed.ToString(),
            ChangedBy = currentUser,
            ChangedDate = DateTime.Now,
            ChangeDescription = "Implementation completed"
        });

        return true;
    }

    public async Task<bool> RollbackAsync(int changeRequestId, string currentUser)
    {
        var changeRequest = await _changeRepo.GetByIdAsync(changeRequestId);
        if (changeRequest == null) return false;

        var oldStatus = changeRequest.Status;
        changeRequest.Status = ChangeStatus.RolledBack;
        changeRequest.ModifiedBy = currentUser;
        await _changeRepo.UpdateAsync(changeRequest);

        await _auditService.LogAsync("Rollback", "ChangeRequest", changeRequestId.ToString(),
            newValues: $"Change {changeRequest.ChangeNumber} rolled back",
            userId: currentUser, username: currentUser);

        await _historyRepo.AddAsync(new ChangeHistory
        {
            ChangeRequestId = changeRequestId,
            FieldName = "Status",
            OldValue = oldStatus.ToString(),
            NewValue = ChangeStatus.RolledBack.ToString(),
            ChangedBy = currentUser,
            ChangedDate = DateTime.Now,
            ChangeDescription = "Change rolled back"
        });

        return true;
    }

    public async Task<IEnumerable<ChangeRequest>> GetFilteredAsync(
        ChangeType? changeType, ChangeStatus? status, ChangeRisk? riskLevel,
        ChangeApprovalStatus? approvalStatus, DateTime? from, DateTime? to)
    {
        var query = _changeRepo.Query().Where(c => !c.IsDeleted);

        if (changeType.HasValue)
            query = query.Where(c => c.ChangeType == changeType.Value);
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);
        if (riskLevel.HasValue)
            query = query.Where(c => c.RiskLevel == riskLevel.Value);
        if (approvalStatus.HasValue)
            query = query.Where(c => c.ApprovalStatus == approvalStatus.Value);
        if (from.HasValue)
            query = query.Where(c => c.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(c => c.CreatedDate <= to.Value);

        return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
    }

    public async Task<IEnumerable<ChangeRequest>> GetPendingApprovalAsync()
    {
        return await _changeRepo.Query()
            .Where(c => !c.IsDeleted && c.Status == ChangeStatus.Submitted && c.ApprovalStatus == ChangeApprovalStatus.Pending)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();
    }

    private async Task<string> GenerateNextNumberAsync()
    {
        return await _changeRepo.GenerateSequentialNumberAsync("Change", "CHG-", nameof(ChangeRequest.ChangeNumber));
    }

    private List<(string FieldName, string? OldValue, string? NewValue)> GetChanges(ChangeRequest old, ChangeRequest updated)
    {
        var changes = new List<(string, string?, string?)>();

        if (old.ChangeType != updated.ChangeType) changes.Add(("ChangeType", old.ChangeType.ToString(), updated.ChangeType.ToString()));
        if (old.RiskLevel != updated.RiskLevel) changes.Add(("RiskLevel", old.RiskLevel.ToString(), updated.RiskLevel.ToString()));
        if (old.Title != updated.Title) changes.Add(("Title", old.Title, updated.Title));
        if (old.Description != updated.Description) changes.Add(("Description", old.Description, updated.Description));
        if (old.Reason != updated.Reason) changes.Add(("Reason", old.Reason, updated.Reason));
        if (old.AffectedCIs != updated.AffectedCIs) changes.Add(("AffectedCIs", old.AffectedCIs, updated.AffectedCIs));
        if (old.ScheduledDate != updated.ScheduledDate) changes.Add(("ScheduledDate", old.ScheduledDate?.ToString(), updated.ScheduledDate?.ToString()));
        if (old.RollbackPlan != updated.RollbackPlan) changes.Add(("RollbackPlan", old.RollbackPlan, updated.RollbackPlan));
        if (old.AssignedToId != updated.AssignedToId) changes.Add(("AssignedToId", old.AssignedToId?.ToString(), updated.AssignedToId?.ToString()));
        if (old.AssignedToName != updated.AssignedToName) changes.Add(("AssignedToName", old.AssignedToName, updated.AssignedToName));

        return changes;
    }
}

public class ProblemService : IProblemService
{
    private readonly IRepository<Problem> _problemRepo;
    private readonly IRepository<ProblemIncident> _problemIncidentRepo;
    private readonly IRepository<Incident> _incidentRepo;
    private readonly IAuditService _auditService;

    public ProblemService(IRepository<Problem> problemRepo, IRepository<ProblemIncident> problemIncidentRepo,
        IRepository<Incident> incidentRepo, IAuditService auditService)
    {
        _problemRepo = problemRepo;
        _problemIncidentRepo = problemIncidentRepo;
        _incidentRepo = incidentRepo;
        _auditService = auditService;
    }

    public async Task<Problem?> GetByIdAsync(int id) => await _problemRepo.GetByIdAsync(id);

    public async Task<Problem?> GetByNumberAsync(string problemNumber) =>
        await _problemRepo.FirstOrDefaultAsync(p => p.ProblemNumber == problemNumber);

    public async Task<IEnumerable<Problem>> GetAllAsync() =>
        await _problemRepo.Query().Where(p => !p.IsDeleted).OrderByDescending(p => p.CreatedDate).ToListAsync();

    public async Task<Problem> CreateAsync(Problem problem, string currentUser)
    {
        problem.ProblemNumber = await GenerateNextNumberAsync();
        problem.Status = ProblemStatus.Logged;
        problem.CreatedBy = currentUser;
        var created = await _problemRepo.AddAsync(problem);

        await _auditService.LogAsync("Create", "Problem", created.Id.ToString(),
            newValues: $"Problem {created.ProblemNumber} created",
            userId: currentUser, username: currentUser);

        return created;
    }

    public async Task<Problem> UpdateAsync(Problem problem, string currentUser)
    {
        var existing = await _problemRepo.GetByIdAsync(problem.Id);
        if (existing == null)
            throw new InvalidOperationException($"Problem {problem.Id} not found.");

        problem.ModifiedBy = currentUser;
        await _problemRepo.UpdateAsync(problem);

        await _auditService.LogAsync("Update", "Problem", problem.Id.ToString(),
            newValues: $"Problem {problem.ProblemNumber} updated",
            userId: currentUser, username: currentUser);

        return problem;
    }

    public async Task<bool> DeleteAsync(int problemId, string currentUser)
    {
        var problem = await _problemRepo.GetByIdAsync(problemId);
        if (problem == null) return false;

        await _problemRepo.SoftDeleteAsync(problem);
        await _auditService.LogAsync("Delete", "Problem", problemId.ToString(),
            oldValues: $"Problem {problem.ProblemNumber} deleted",
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<bool> LinkIncidentAsync(int problemId, int incidentId, string? notes)
    {
        var problem = await _problemRepo.GetByIdAsync(problemId);
        var incident = await _incidentRepo.GetByIdAsync(incidentId);
        if (problem == null || incident == null) return false;

        var existing = await _problemIncidentRepo.FirstOrDefaultAsync(
            pi => pi.ProblemId == problemId && pi.IncidentId == incidentId);
        if (existing != null) return false;

        var link = new ProblemIncident
        {
            ProblemId = problemId,
            IncidentId = incidentId,
            Notes = notes
        };

        await _problemIncidentRepo.AddAsync(link);

        incident.ProblemId = problemId;
        await _incidentRepo.UpdateAsync(incident);

        await _auditService.LogAsync("LinkIncident", "Problem", problemId.ToString(),
            newValues: $"Incident {incident.IncidentNumber} linked to problem {problem.ProblemNumber}",
            userId: problem.CreatedBy, username: problem.CreatedBy ?? "System");

        return true;
    }

    public async Task<bool> UnlinkIncidentAsync(int problemId, int incidentId)
    {
        var link = await _problemIncidentRepo.FirstOrDefaultAsync(
            pi => pi.ProblemId == problemId && pi.IncidentId == incidentId);
        if (link == null) return false;

        await _problemIncidentRepo.DeleteAsync(link);

        var incident = await _incidentRepo.GetByIdAsync(incidentId);
        if (incident != null)
        {
            incident.ProblemId = null;
            await _incidentRepo.UpdateAsync(incident);
        }

        await _auditService.LogAsync("UnlinkIncident", "Problem", problemId.ToString(),
            newValues: $"Incident {incidentId} unlinked from problem {problemId}",
            userId: "System", username: "System");

        return true;
    }

    public async Task<IEnumerable<Incident>> GetLinkedIncidentsAsync(int problemId)
    {
        var links = await _problemIncidentRepo.Query()
            .Where(pi => pi.ProblemId == problemId)
            .ToListAsync();

        var incidentIds = links.Select(l => l.IncidentId).ToList();

        return await _incidentRepo.Query()
            .Where(i => incidentIds.Contains(i.Id) && !i.IsDeleted)
            .OrderByDescending(i => i.IncidentDate)
            .ToListAsync();
    }

    public async Task<bool> IdentifyRootCauseAsync(int problemId, string rootCause, string currentUser)
    {
        var problem = await _problemRepo.GetByIdAsync(problemId);
        if (problem == null) return false;

        problem.RootCause = rootCause;
        problem.Status = ProblemStatus.RootCauseIdentified;
        problem.IdentifiedDate = DateTime.Now;
        problem.ModifiedBy = currentUser;
        await _problemRepo.UpdateAsync(problem);

        await _auditService.LogAsync("IdentifyRootCause", "Problem", problemId.ToString(),
            newValues: $"Root cause identified: {rootCause}",
            userId: currentUser, username: currentUser);

        return true;
    }

    public async Task<bool> SetWorkaroundAsync(int problemId, string workaround, string currentUser)
    {
        var problem = await _problemRepo.GetByIdAsync(problemId);
        if (problem == null) return false;

        problem.Workaround = workaround;
        problem.Status = ProblemStatus.WorkaroundKnown;
        problem.ModifiedBy = currentUser;
        await _problemRepo.UpdateAsync(problem);

        await _auditService.LogAsync("SetWorkaround", "Problem", problemId.ToString(),
            newValues: $"Workaround set: {workaround}",
            userId: currentUser, username: currentUser);

        return true;
    }

    public async Task<bool> SetPermanentSolutionAsync(int problemId, string permanentSolution, string currentUser)
    {
        var problem = await _problemRepo.GetByIdAsync(problemId);
        if (problem == null) return false;

        problem.PermanentSolution = permanentSolution;
        problem.ModifiedBy = currentUser;
        await _problemRepo.UpdateAsync(problem);

        await _auditService.LogAsync("SetPermanentSolution", "Problem", problemId.ToString(),
            newValues: $"Permanent solution set: {permanentSolution}",
            userId: currentUser, username: currentUser);

        return true;
    }

    public async Task<bool> ResolveAsync(int problemId, string resolutionNotes, string currentUser)
    {
        var problem = await _problemRepo.GetByIdAsync(problemId);
        if (problem == null) return false;

        problem.Status = ProblemStatus.Resolved;
        problem.ResolutionNotes = resolutionNotes;
        problem.ResolvedDate = DateTime.Now;
        problem.ModifiedBy = currentUser;
        await _problemRepo.UpdateAsync(problem);

        await _auditService.LogAsync("Resolve", "Problem", problemId.ToString(),
            newValues: $"Problem resolved: {resolutionNotes}",
            userId: currentUser, username: currentUser);

        return true;
    }

    public async Task<bool> CloseAsync(int problemId, string currentUser)
    {
        var problem = await _problemRepo.GetByIdAsync(problemId);
        if (problem == null) return false;

        problem.Status = ProblemStatus.Closed;
        problem.ModifiedBy = currentUser;
        await _problemRepo.UpdateAsync(problem);

        await _auditService.LogAsync("Close", "Problem", problemId.ToString(),
            newValues: "Problem closed",
            userId: currentUser, username: currentUser);

        return true;
    }

    private async Task<string> GenerateNextNumberAsync()
    {
        return await _problemRepo.GenerateSequentialNumberAsync("Problem", "PRB-", nameof(Problem.ProblemNumber));
    }
}

public class KnownErrorService : IKnownErrorService
{
    private readonly IRepository<KnownError> _knownErrorRepo;
    private readonly IAuditService _auditService;

    public KnownErrorService(IRepository<KnownError> knownErrorRepo, IAuditService auditService)
    {
        _knownErrorRepo = knownErrorRepo;
        _auditService = auditService;
    }

    public async Task<KnownError?> GetByIdAsync(int id) => await _knownErrorRepo.GetByIdAsync(id);

    public async Task<KnownError?> GetByNumberAsync(string knownErrorNumber) =>
        await _knownErrorRepo.FirstOrDefaultAsync(k => k.KnownErrorNumber == knownErrorNumber);

    public async Task<IEnumerable<KnownError>> GetAllAsync() =>
        await _knownErrorRepo.Query().Where(k => !k.IsDeleted).OrderByDescending(k => k.CreatedDate).ToListAsync();

    public async Task<KnownError> CreateAsync(KnownError knownError, string currentUser)
    {
        knownError.KnownErrorNumber = await GenerateNextNumberAsync();
        knownError.Status = KnownErrorStatus.Active;
        knownError.CreatedByName = currentUser;
        var created = await _knownErrorRepo.AddAsync(knownError);

        await _auditService.LogAsync("Create", "KnownError", created.Id.ToString(),
            newValues: $"Known error {created.KnownErrorNumber} created",
            userId: currentUser, username: currentUser);

        return created;
    }

    public async Task<KnownError> UpdateAsync(KnownError knownError, string currentUser)
    {
        var existing = await _knownErrorRepo.GetByIdAsync(knownError.Id);
        if (existing == null)
            throw new InvalidOperationException($"Known error {knownError.Id} not found.");

        knownError.ModifiedBy = currentUser;
        await _knownErrorRepo.UpdateAsync(knownError);

        await _auditService.LogAsync("Update", "KnownError", knownError.Id.ToString(),
            newValues: $"Known error {knownError.KnownErrorNumber} updated",
            userId: currentUser, username: currentUser);

        return knownError;
    }

    public async Task<bool> DeleteAsync(int knownErrorId, string currentUser)
    {
        var knownError = await _knownErrorRepo.GetByIdAsync(knownErrorId);
        if (knownError == null) return false;

        await _knownErrorRepo.SoftDeleteAsync(knownError);
        await _auditService.LogAsync("Delete", "KnownError", knownErrorId.ToString(),
            oldValues: $"Known error {knownError.KnownErrorNumber} deleted",
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<IEnumerable<KnownError>> SearchAsync(string query)
    {
        return await _knownErrorRepo.Query()
            .Where(k => !k.IsDeleted &&
                (k.KnownErrorNumber.Contains(query) ||
                 k.Title.Contains(query) ||
                 k.Symptoms.Contains(query) ||
                 (k.Workaround != null && k.Workaround.Contains(query)) ||
                 (k.RelatedSoftware != null && k.RelatedSoftware.Contains(query)) ||
                 (k.RelatedEquipment != null && k.RelatedEquipment.Contains(query))))
            .OrderByDescending(k => k.CreatedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<KnownError>> GetByStatusAsync(KnownErrorStatus status)
    {
        return await _knownErrorRepo.Query()
            .Where(k => !k.IsDeleted && k.Status == status)
            .OrderByDescending(k => k.CreatedDate)
            .ToListAsync();
    }

    private async Task<string> GenerateNextNumberAsync()
    {
        return await _knownErrorRepo.GenerateSequentialNumberAsync("KnownError", "KE-", nameof(KnownError.KnownErrorNumber));
    }
}

public class ConfigurationItemService : IConfigurationItemService
{
    private readonly IRepository<ConfigurationItem> _ciRepo;
    private readonly IRepository<Incident> _incidentRepo;
    private readonly IRepository<ChangeRequest> _changeRepo;
    private readonly IAuditService _auditService;

    public ConfigurationItemService(IRepository<ConfigurationItem> ciRepo, IRepository<Incident> incidentRepo,
        IRepository<ChangeRequest> changeRepo, IAuditService auditService)
    {
        _ciRepo = ciRepo;
        _incidentRepo = incidentRepo;
        _changeRepo = changeRepo;
        _auditService = auditService;
    }

    public async Task<ConfigurationItem?> GetByIdAsync(int id) => await _ciRepo.GetByIdAsync(id);

    public async Task<ConfigurationItem?> GetByNumberAsync(string ciNumber) =>
        await _ciRepo.FirstOrDefaultAsync(ci => ci.CiNumber == ciNumber);

    public async Task<IEnumerable<ConfigurationItem>> GetAllAsync() =>
        await _ciRepo.Query().Where(ci => !ci.IsDeleted).OrderBy(ci => ci.Name).ToListAsync();

    public async Task<ConfigurationItem> CreateAsync(ConfigurationItem ci, string currentUser)
    {
        ci.CiNumber = await GenerateNextCiNumberAsync();
        ci.CreatedBy = currentUser;
        var created = await _ciRepo.AddAsync(ci);

        await _auditService.LogAsync("Create", "ConfigurationItem", created.Id.ToString(),
            newValues: $"CI {created.CiNumber} - {created.Name} created",
            userId: currentUser, username: currentUser);

        return created;
    }

    public async Task<ConfigurationItem> UpdateAsync(ConfigurationItem ci, string currentUser)
    {
        var existing = await _ciRepo.GetByIdAsync(ci.Id);
        if (existing == null)
            throw new InvalidOperationException($"Configuration item {ci.Id} not found.");

        ci.ModifiedBy = currentUser;
        await _ciRepo.UpdateAsync(ci);

        await _auditService.LogAsync("Update", "ConfigurationItem", ci.Id.ToString(),
            newValues: $"CI {ci.CiNumber} - {ci.Name} updated",
            userId: currentUser, username: currentUser);

        return ci;
    }

    public async Task<bool> DeleteAsync(int ciId, string currentUser)
    {
        var ci = await _ciRepo.GetByIdAsync(ciId);
        if (ci == null) return false;

        await _ciRepo.SoftDeleteAsync(ci);
        await _auditService.LogAsync("Delete", "ConfigurationItem", ciId.ToString(),
            oldValues: $"CI {ci.CiNumber} - {ci.Name} deleted",
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<IEnumerable<ConfigurationItem>> SearchAsync(string query)
    {
        return await _ciRepo.Query()
            .Where(ci => !ci.IsDeleted &&
                (ci.CiNumber.Contains(query) ||
                 ci.Name.Contains(query) ||
                 (ci.Description != null && ci.Description.Contains(query)) ||
                 (ci.SerialNumber != null && ci.SerialNumber.Contains(query)) ||
                 (ci.AssetTag != null && ci.AssetTag.Contains(query)) ||
                 (ci.ComputerName != null && ci.ComputerName.Contains(query)) ||
                 (ci.IpAddress != null && ci.IpAddress.Contains(query)) ||
                 (ci.Department != null && ci.Department.Contains(query)) ||
                 (ci.Manufacturer != null && ci.Manufacturer.Contains(query)) ||
                 (ci.Model != null && ci.Model.Contains(query))))
            .OrderBy(ci => ci.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConfigurationItem>> GetByTypeAsync(EquipmentType equipmentType)
    {
        return await _ciRepo.Query()
            .Where(ci => !ci.IsDeleted && ci.EquipmentType == equipmentType)
            .OrderBy(ci => ci.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConfigurationItem>> GetByDepartmentAsync(string department)
    {
        return await _ciRepo.Query()
            .Where(ci => !ci.IsDeleted && ci.Department != null && ci.Department == department)
            .OrderBy(ci => ci.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Incident>> GetIncidentsAsync(int ciId)
    {
        return await _incidentRepo.Query()
            .Where(i => !i.IsDeleted && i.ConfigurationItemId == ciId)
            .OrderByDescending(i => i.IncidentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChangeRequest>> GetChangesAsync(int ciId)
    {
        var ci = await _ciRepo.GetByIdAsync(ciId);
        if (ci == null) return Enumerable.Empty<ChangeRequest>();

        return await _changeRepo.Query()
            .Where(c => !c.IsDeleted && c.AffectedCIs != null && c.AffectedCIs.Contains(ci.CiNumber))
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();
    }

    private async Task<string> GenerateNextCiNumberAsync()
    {
        return await _ciRepo.GenerateSequentialNumberAsync("ConfigurationItem", "CI-", nameof(ConfigurationItem.CiNumber));
    }
}

public class ServiceRequestService : IServiceRequestService
{
    private readonly IRepository<ServiceRequest> _serviceRequestRepo;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public ServiceRequestService(IRepository<ServiceRequest> serviceRequestRepo,
        IAuditService auditService, INotificationService notificationService)
    {
        _serviceRequestRepo = serviceRequestRepo;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<ServiceRequest?> GetByIdAsync(int id) => await _serviceRequestRepo.GetByIdAsync(id);

    public async Task<ServiceRequest?> GetByNumberAsync(string requestNumber) =>
        await _serviceRequestRepo.FirstOrDefaultAsync(sr => sr.RequestNumber == requestNumber);

    public async Task<IEnumerable<ServiceRequest>> GetAllAsync() =>
        await _serviceRequestRepo.Query().Where(sr => !sr.IsDeleted).OrderByDescending(sr => sr.CreatedDate).ToListAsync();

    public async Task<ServiceRequest> CreateAsync(ServiceRequest serviceRequest, string currentUser)
    {
        serviceRequest.RequestNumber = await GenerateNextNumberAsync();
        serviceRequest.Status = ServiceRequestStatus.New;
        serviceRequest.ApprovalStatus = ApprovalStatus.Pending;
        serviceRequest.CreatedBy = currentUser;
        var created = await _serviceRequestRepo.AddAsync(serviceRequest);

        await _auditService.LogAsync("Create", "ServiceRequest", created.Id.ToString(),
            newValues: $"Service request {created.RequestNumber} created",
            userId: currentUser, username: currentUser);

        return created;
    }

    public async Task<ServiceRequest> UpdateAsync(ServiceRequest serviceRequest, string currentUser)
    {
        var existing = await _serviceRequestRepo.GetByIdAsync(serviceRequest.Id);
        if (existing == null)
            throw new InvalidOperationException($"Service request {serviceRequest.Id} not found.");

        serviceRequest.ModifiedBy = currentUser;
        await _serviceRequestRepo.UpdateAsync(serviceRequest);

        await _auditService.LogAsync("Update", "ServiceRequest", serviceRequest.Id.ToString(),
            newValues: $"Service request {serviceRequest.RequestNumber} updated",
            userId: currentUser, username: currentUser);

        return serviceRequest;
    }

    public async Task<bool> DeleteAsync(int serviceRequestId, string currentUser)
    {
        var serviceRequest = await _serviceRequestRepo.GetByIdAsync(serviceRequestId);
        if (serviceRequest == null) return false;

        await _serviceRequestRepo.SoftDeleteAsync(serviceRequest);
        await _auditService.LogAsync("Delete", "ServiceRequest", serviceRequestId.ToString(),
            oldValues: $"Service request {serviceRequest.RequestNumber} deleted",
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<bool> SubmitAsync(int serviceRequestId, string currentUser)
    {
        var serviceRequest = await _serviceRequestRepo.GetByIdAsync(serviceRequestId);
        if (serviceRequest == null || serviceRequest.Status != ServiceRequestStatus.New) return false;

        serviceRequest.Status = ServiceRequestStatus.Submitted;
        serviceRequest.ModifiedBy = currentUser;
        await _serviceRequestRepo.UpdateAsync(serviceRequest);

        await _auditService.LogAsync("Submit", "ServiceRequest", serviceRequestId.ToString(),
            newValues: $"Service request {serviceRequest.RequestNumber} submitted for approval",
            userId: currentUser, username: currentUser);

        return true;
    }

    public async Task<bool> ApproveAsync(int serviceRequestId, string approverName, string? notes, string currentUser)
    {
        var serviceRequest = await _serviceRequestRepo.GetByIdAsync(serviceRequestId);
        if (serviceRequest == null || serviceRequest.Status != ServiceRequestStatus.Submitted) return false;

        serviceRequest.Status = ServiceRequestStatus.Approved;
        serviceRequest.ApprovalStatus = ApprovalStatus.Approved;
        serviceRequest.ApproverName = approverName;
        serviceRequest.ApprovalNotes = notes;
        serviceRequest.ApprovalDate = DateTime.Now;
        serviceRequest.ModifiedBy = currentUser;
        await _serviceRequestRepo.UpdateAsync(serviceRequest);

        await _auditService.LogAsync("Approve", "ServiceRequest", serviceRequestId.ToString(),
            newValues: $"Service request {serviceRequest.RequestNumber} approved by {approverName}",
            userId: currentUser, username: currentUser);

        return true;
    }

    public async Task<bool> RejectAsync(int serviceRequestId, string approverName, string? notes, string currentUser)
    {
        var serviceRequest = await _serviceRequestRepo.GetByIdAsync(serviceRequestId);
        if (serviceRequest == null || serviceRequest.Status != ServiceRequestStatus.Submitted) return false;

        serviceRequest.Status = ServiceRequestStatus.Rejected;
        serviceRequest.ApprovalStatus = ApprovalStatus.Rejected;
        serviceRequest.ApproverName = approverName;
        serviceRequest.ApprovalNotes = notes;
        serviceRequest.ApprovalDate = DateTime.Now;
        serviceRequest.ModifiedBy = currentUser;
        await _serviceRequestRepo.UpdateAsync(serviceRequest);

        await _auditService.LogAsync("Reject", "ServiceRequest", serviceRequestId.ToString(),
            newValues: $"Service request {serviceRequest.RequestNumber} rejected by {approverName}",
            userId: currentUser, username: currentUser);

        return true;
    }

    public async Task<bool> StartAsync(int serviceRequestId, string currentUser)
    {
        var serviceRequest = await _serviceRequestRepo.GetByIdAsync(serviceRequestId);
        if (serviceRequest == null || serviceRequest.Status != ServiceRequestStatus.Approved) return false;

        serviceRequest.Status = ServiceRequestStatus.InProgress;
        serviceRequest.ModifiedBy = currentUser;
        await _serviceRequestRepo.UpdateAsync(serviceRequest);

        await _auditService.LogAsync("Start", "ServiceRequest", serviceRequestId.ToString(),
            newValues: $"Service request {serviceRequest.RequestNumber} started",
            userId: currentUser, username: currentUser);

        return true;
    }

    public async Task<bool> CompleteAsync(int serviceRequestId, string? completionNotes, string currentUser)
    {
        var serviceRequest = await _serviceRequestRepo.GetByIdAsync(serviceRequestId);
        if (serviceRequest == null || serviceRequest.Status != ServiceRequestStatus.InProgress) return false;

        serviceRequest.Status = ServiceRequestStatus.Completed;
        serviceRequest.CompletionDate = DateTime.Now;
        serviceRequest.CompletionNotes = completionNotes;
        serviceRequest.ModifiedBy = currentUser;
        await _serviceRequestRepo.UpdateAsync(serviceRequest);

        await _auditService.LogAsync("Complete", "ServiceRequest", serviceRequestId.ToString(),
            newValues: $"Service request {serviceRequest.RequestNumber} completed",
            userId: currentUser, username: currentUser);

        return true;
    }

    public async Task<bool> CancelAsync(int serviceRequestId, string currentUser)
    {
        var serviceRequest = await _serviceRequestRepo.GetByIdAsync(serviceRequestId);
        if (serviceRequest == null) return false;

        serviceRequest.Status = ServiceRequestStatus.Cancelled;
        serviceRequest.ModifiedBy = currentUser;
        await _serviceRequestRepo.UpdateAsync(serviceRequest);

        await _auditService.LogAsync("Cancel", "ServiceRequest", serviceRequestId.ToString(),
            newValues: $"Service request {serviceRequest.RequestNumber} cancelled",
            userId: currentUser, username: currentUser);

        return true;
    }

    public async Task<IEnumerable<ServiceRequest>> GetFilteredAsync(
        ServiceRequestType? type, ServiceRequestStatus? status,
        ApprovalStatus? approvalStatus, DateTime? from, DateTime? to, string? section)
    {
        var query = _serviceRequestRepo.Query().Where(sr => !sr.IsDeleted);

        if (type.HasValue)
            query = query.Where(sr => sr.RequestType == type.Value);
        if (status.HasValue)
            query = query.Where(sr => sr.Status == status.Value);
        if (approvalStatus.HasValue)
            query = query.Where(sr => sr.ApprovalStatus == approvalStatus.Value);
        if (from.HasValue)
            query = query.Where(sr => sr.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(sr => sr.CreatedDate <= to.Value);
        if (!string.IsNullOrWhiteSpace(section))
            query = query.Where(sr => sr.RequesterSection == section);

        return await query.OrderByDescending(sr => sr.CreatedDate).ToListAsync();
    }

    public async Task<IEnumerable<ServiceRequest>> GetPendingApprovalAsync()
    {
        return await _serviceRequestRepo.Query()
            .Where(sr => !sr.IsDeleted && sr.Status == ServiceRequestStatus.Submitted && sr.ApprovalStatus == ApprovalStatus.Pending)
            .OrderByDescending(sr => sr.CreatedDate)
            .ToListAsync();
    }

    private async Task<string> GenerateNextNumberAsync()
    {
        return await _serviceRequestRepo.GenerateSequentialNumberAsync("ServiceRequest", "SR-", nameof(ServiceRequest.RequestNumber));
    }
}

public class SlaService : ISlaService
{
    private readonly IRepository<SlaDefinition> _slaRepo;
    private readonly IRepository<Incident> _incidentRepo;
    private readonly IAuditService _auditService;

    public SlaService(IRepository<SlaDefinition> slaRepo, IRepository<Incident> incidentRepo, IAuditService auditService)
    {
        _slaRepo = slaRepo;
        _incidentRepo = incidentRepo;
        _auditService = auditService;
    }

    public async Task<SlaDefinition?> GetByIdAsync(int id) => await _slaRepo.GetByIdAsync(id);

    public async Task<IEnumerable<SlaDefinition>> GetAllAsync() =>
        await _slaRepo.Query().Where(s => !s.IsDeleted).OrderBy(s => s.Name).ToListAsync();

    public async Task<SlaDefinition> CreateAsync(SlaDefinition sla, string currentUser)
    {
        sla.IsActive = true;
        sla.CreatedBy = currentUser;
        var created = await _slaRepo.AddAsync(sla);

        await _auditService.LogAsync("Create", "SlaDefinition", created.Id.ToString(),
            newValues: $"SLA definition '{created.Name}' created",
            userId: currentUser, username: currentUser);

        return created;
    }

    public async Task<SlaDefinition> UpdateAsync(SlaDefinition sla, string currentUser)
    {
        var existing = await _slaRepo.GetByIdAsync(sla.Id);
        if (existing == null)
            throw new InvalidOperationException($"SLA definition {sla.Id} not found.");

        sla.ModifiedBy = currentUser;
        await _slaRepo.UpdateAsync(sla);

        await _auditService.LogAsync("Update", "SlaDefinition", sla.Id.ToString(),
            newValues: $"SLA definition '{sla.Name}' updated",
            userId: currentUser, username: currentUser);

        return sla;
    }

    public async Task<bool> DeleteAsync(int slaId, string currentUser)
    {
        var sla = await _slaRepo.GetByIdAsync(slaId);
        if (sla == null) return false;

        await _slaRepo.SoftDeleteAsync(sla);
        await _auditService.LogAsync("Delete", "SlaDefinition", slaId.ToString(),
            oldValues: $"SLA definition '{sla.Name}' deleted",
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<(double remainingMinutes, bool isBreached)> CalculateRemainingTimeAsync(int incidentId)
    {
        var incident = await _incidentRepo.Query()
            .Include(i => i.ConfigurationItem)
            .FirstOrDefaultAsync(i => i.Id == incidentId);

        if (incident == null)
            return (0, true);

        var sla = await MatchSlaDefinitionAsync(incident.Priority, incident.Category,
            incident.EquipmentType, null);
        if (sla == null)
            return (0, true);

        if (!incident.AcknowledgedDate.HasValue)
        {
            var elapsedFromCreation = (DateTime.Now - incident.CreatedDate).TotalMinutes;
            var remaining = sla.ResolutionTimeMinutes - elapsedFromCreation;
            return (remaining, remaining <= 0);
        }

        var totalElapsed = (DateTime.Now - incident.AcknowledgedDate.Value).TotalMinutes;
        var remainingMinutes = sla.ResolutionTimeMinutes - totalElapsed;
        return (remainingMinutes, remainingMinutes <= 0);
    }

    public async Task<bool> CheckSlaComplianceAsync(int incidentId)
    {
        var (remaining, isBreached) = await CalculateRemainingTimeAsync(incidentId);
        return !isBreached;
    }

    public async Task<SlaStatusDto> GetSlaStatusAsync(int incidentId)
    {
        var incident = await _incidentRepo.Query()
            .Include(i => i.ConfigurationItem)
            .FirstOrDefaultAsync(i => i.Id == incidentId);

        if (incident == null)
            return new SlaStatusDto { RemainingMinutes = 0, ElapsedMinutes = 0, IsBreached = true, SlaName = null, ResolutionTimeMinutes = 0 };

        var sla = await MatchSlaDefinitionAsync(incident.Priority, incident.Category,
            incident.EquipmentType, null);

        if (sla == null)
            return new SlaStatusDto { RemainingMinutes = 0, ElapsedMinutes = 0, IsBreached = true, SlaName = null, ResolutionTimeMinutes = 0 };

        double elapsedMinutes;
        if (incident.AcknowledgedDate.HasValue)
            elapsedMinutes = (DateTime.Now - incident.AcknowledgedDate.Value).TotalMinutes;
        else
            elapsedMinutes = (DateTime.Now - incident.CreatedDate).TotalMinutes;

        var remainingMinutes = sla.ResolutionTimeMinutes - elapsedMinutes;

        return new SlaStatusDto
        {
            RemainingMinutes = Math.Round(remainingMinutes, 2),
            ElapsedMinutes = Math.Round(elapsedMinutes, 2),
            IsBreached = remainingMinutes <= 0,
            SlaName = sla.Name,
            ResolutionTimeMinutes = sla.ResolutionTimeMinutes
        };
    }

    public async Task<SlaDefinition?> MatchSlaDefinitionAsync(
        Priority priority, string? category, EquipmentType? equipmentType, string? department)
    {
        var allSlas = await _slaRepo.Query()
            .Where(s => !s.IsDeleted && s.IsActive)
            .ToListAsync();

        var exactMatch = allSlas.FirstOrDefault(s =>
            s.Priority == priority &&
            s.Category == category &&
            s.EquipmentType == equipmentType &&
            s.Department == department);

        if (exactMatch != null) return exactMatch;

        var priorityEquipmentMatch = allSlas.FirstOrDefault(s =>
            s.Priority == priority &&
            s.EquipmentType == equipmentType &&
            s.Category == category &&
            s.Department == null);

        if (priorityEquipmentMatch != null) return priorityEquipmentMatch;

        var priorityCategoryMatch = allSlas.FirstOrDefault(s =>
            s.Priority == priority &&
            s.Category == category &&
            s.EquipmentType == null &&
            s.Department == null);

        if (priorityCategoryMatch != null) return priorityCategoryMatch;

        var priorityOnlyMatch = allSlas.FirstOrDefault(s =>
            s.Priority == priority &&
            s.Category == null &&
            s.EquipmentType == null &&
            s.Department == null);

        return priorityOnlyMatch;
    }
}

public class EscalationService : IEscalationService
{
    private readonly IRepository<EscalationRule> _ruleRepo;
    private readonly IRepository<Incident> _incidentRepo;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ISlaService _slaService;

    public EscalationService(IRepository<EscalationRule> ruleRepo, IRepository<Incident> incidentRepo,
        IAuditService auditService, INotificationService notificationService, ISlaService slaService)
    {
        _ruleRepo = ruleRepo;
        _incidentRepo = incidentRepo;
        _auditService = auditService;
        _notificationService = notificationService;
        _slaService = slaService;
    }

    public async Task<EscalationRule?> GetRuleByIdAsync(int id) => await _ruleRepo.GetByIdAsync(id);

    public async Task<IEnumerable<EscalationRule>> GetAllRulesAsync() =>
        await _ruleRepo.Query().Where(r => !r.IsDeleted).OrderBy(r => r.Name).ToListAsync();

    public async Task<EscalationRule> CreateRuleAsync(EscalationRule rule, string currentUser)
    {
        rule.IsActive = true;
        rule.CreatedBy = currentUser;
        var created = await _ruleRepo.AddAsync(rule);

        await _auditService.LogAsync("Create", "EscalationRule", created.Id.ToString(),
            newValues: $"Escalation rule '{created.Name}' created",
            userId: currentUser, username: currentUser);

        return created;
    }

    public async Task<EscalationRule> UpdateRuleAsync(EscalationRule rule, string currentUser)
    {
        var existing = await _ruleRepo.GetByIdAsync(rule.Id);
        if (existing == null)
            throw new InvalidOperationException($"Escalation rule {rule.Id} not found.");

        rule.ModifiedBy = currentUser;
        await _ruleRepo.UpdateAsync(rule);

        await _auditService.LogAsync("Update", "EscalationRule", rule.Id.ToString(),
            newValues: $"Escalation rule '{rule.Name}' updated",
            userId: currentUser, username: currentUser);

        return rule;
    }

    public async Task<bool> DeleteRuleAsync(int ruleId, string currentUser)
    {
        var rule = await _ruleRepo.GetByIdAsync(ruleId);
        if (rule == null) return false;

        await _ruleRepo.SoftDeleteAsync(rule);
        await _auditService.LogAsync("Delete", "EscalationRule", ruleId.ToString(),
            oldValues: $"Escalation rule '{rule.Name}' deleted",
            userId: currentUser, username: currentUser);
        return true;
    }

    public async Task<int> ProcessEscalationsAsync()
    {
        var escalatedCount = 0;
        var openIncidents = await _incidentRepo.Query()
            .Where(i => !i.IsDeleted &&
                i.WorkflowStatus != WorkflowStatus.Resolved &&
                i.WorkflowStatus != WorkflowStatus.Closed &&
                i.WorkflowStatus != WorkflowStatus.Cancelled)
            .ToListAsync();

        var activeRules = await _ruleRepo.Query()
            .Where(r => !r.IsDeleted && r.IsActive)
            .ToListAsync();

        foreach (var incident in openIncidents)
        {
            foreach (var rule in activeRules)
            {
                if (!RuleMatchesIncident(rule, incident)) continue;

                var slaStatus = await _slaService.GetSlaStatusAsync(incident.Id);
                if (slaStatus == null) continue;

                var elapsedMinutes = slaStatus.ElapsedMinutes;
                var isBreached = slaStatus.IsBreached;

                if (isBreached && elapsedMinutes >= rule.BreachMinutes && incident.EscalationLevel < rule.TargetLevel)
                {
                    await EscalateIncidentAsync(incident.Id, rule.TargetLevel, "System");
                    escalatedCount++;
                    break;
                }
                else if (elapsedMinutes >= rule.WarningMinutes && elapsedMinutes < rule.BreachMinutes && incident.EscalationLevel < EscalationLevel.Level1)
                {
                    await _notificationService.SendSlaWarningAsync(
                        incident.AssignedSpecialistId ?? 0, "System", "Incident",
                        incident.Id, incident.IncidentNumber, rule.BreachMinutes - elapsedMinutes);
                }
            }
        }

        return escalatedCount;
    }

    public async Task<bool> EscalateIncidentAsync(int incidentId, EscalationLevel targetLevel, string currentUser)
    {
        var incident = await _incidentRepo.GetByIdAsync(incidentId);
        if (incident == null) return false;

        var oldLevel = incident.EscalationLevel;
        incident.EscalationLevel = targetLevel;
        incident.LastEscalationCheck = DateTime.Now;
        incident.ModifiedBy = currentUser;
        await _incidentRepo.UpdateAsync(incident);

        await _auditService.LogAsync("Escalate", "Incident", incidentId.ToString(),
            oldValues: $"EscalationLevel: {oldLevel}",
            newValues: $"EscalationLevel: {targetLevel}",
            userId: currentUser, username: currentUser);

        await _notificationService.SendEscalationNotificationAsync(
            incident.AssignedSpecialistId ?? 0, currentUser, "Incident",
            incidentId, incident.IncidentNumber, targetLevel);

        return true;
    }

    private bool RuleMatchesIncident(EscalationRule rule, Incident incident)
    {
        if (rule.Priority.HasValue && rule.Priority.Value != incident.Priority) return false;
        if (!string.IsNullOrWhiteSpace(rule.Category) && rule.Category != incident.Category) return false;
        if (rule.EquipmentType.HasValue && rule.EquipmentType.Value != incident.EquipmentType) return false;
        return true;
    }
}

public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _notificationRepo;

    public NotificationService(IRepository<Notification> notificationRepo)
    {
        _notificationRepo = notificationRepo;
    }

    public async Task<Notification> CreateAsync(Notification notification, string currentUser)
    {
        notification.NotificationDate = DateTime.Now;
        notification.IsRead = false;
        notification.CreatedBy = currentUser;
        return await _notificationRepo.AddAsync(notification);
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
    {
        return await _notificationRepo.Query()
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.NotificationDate)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _notificationRepo.CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId)
    {
        var notification = await _notificationRepo.GetByIdAsync(notificationId);
        if (notification == null) return false;

        notification.IsRead = true;
        notification.ReadDate = DateTime.Now;
        await _notificationRepo.UpdateAsync(notification);
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var unreadNotifications = await _notificationRepo.Query()
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadDate = DateTime.Now;
            await _notificationRepo.UpdateAsync(notification);
        }

        return true;
    }

    public async Task SendAssignmentNotificationAsync(int userId, string userName, string entityType, int entityId, string entityNumber)
    {
        var notification = new Notification
        {
            NotificationType = NotificationType.Assignment,
            UserId = userId,
            UserName = userName,
            Title = $"تم تعيين {entityType}",
            Message = $"تم تعيين {entityType} {entityNumber} لك",
            EntityType = entityType,
            EntityId = entityId,
            CreatedBy = userName
        };

        await _notificationRepo.AddAsync(notification);
    }

    public async Task SendEscalationNotificationAsync(int userId, string userName, string entityType, int entityId, string entityNumber, EscalationLevel level)
    {
        var notification = new Notification
        {
            NotificationType = NotificationType.Escalation,
            UserId = userId,
            UserName = userName,
            Title = $"تصعيد {entityType}",
            Message = $"تم تصعيد {entityType} {entityNumber} إلى {level.ToArabicString()}",
            EntityType = entityType,
            EntityId = entityId,
            CreatedBy = userName
        };

        await _notificationRepo.AddAsync(notification);
    }

    public async Task SendSlaWarningAsync(int userId, string userName, string entityType, int entityId, string entityNumber, double remainingMinutes)
    {
        var notification = new Notification
        {
            NotificationType = NotificationType.SlaWarning,
            UserId = userId,
            UserName = userName,
            Title = $"تنبيه اتفاقية مستوى الخدمة - {entityType} {entityNumber}",
            Message = $"تنبيه: متبقي {Math.Round(remainingMinutes, 0)} دقيقة لـ {entityType} {entityNumber}",
            EntityType = entityType,
            EntityId = entityId,
            CreatedBy = userName
        };

        await _notificationRepo.AddAsync(notification);
    }

    public async Task SendSlaBreachAsync(int userId, string userName, string entityType, int entityId, string entityNumber)
    {
        var notification = new Notification
        {
            NotificationType = NotificationType.SlaBreach,
            UserId = userId,
            UserName = userName,
            Title = $"خرق اتفاقية مستوى الخدمة - {entityType} {entityNumber}",
            Message = $"تم خرق اتفاقية مستوى الخدمة لـ {entityType} {entityNumber}",
            EntityType = entityType,
            EntityId = entityId,
            CreatedBy = userName
        };

        await _notificationRepo.AddAsync(notification);
    }
}

public class ItsmAnalyticsService : IItsmAnalyticsService
{
    private readonly IRepository<Incident> _incidentRepo;
    private readonly IRepository<Problem> _problemRepo;
    private readonly IRepository<ChangeRequest> _changeRepo;

    public ItsmAnalyticsService(IRepository<Incident> incidentRepo, IRepository<Problem> problemRepo,
        IRepository<ChangeRequest> changeRepo)
    {
        _incidentRepo = incidentRepo;
        _problemRepo = problemRepo;
        _changeRepo = changeRepo;
    }

    public async Task<IEnumerable<object>> GetIncidentTrendAsync()
    {
        var thirtyDaysAgo = DateTime.Today.AddDays(-30);
        var incidents = await _incidentRepo.Query()
            .Where(i => !i.IsDeleted && i.CreatedDate >= thirtyDaysAgo)
            .ToListAsync();

        var trend = Enumerable.Range(0, 30)
            .Select(offset => DateTime.Today.AddDays(-29 + offset))
            .Select(date => new
            {
                Date = date.ToString("yyyy-MM-dd"),
                Count = incidents.Count(i => i.CreatedDate.Date == date.Date)
            })
            .Cast<object>()
            .ToList();

        return trend;
    }

    public async Task<IEnumerable<object>> GetTopEquipmentFailuresAsync()
    {
        var incidents = await _incidentRepo.Query()
            .Where(i => !i.IsDeleted && i.EquipmentType.HasValue)
            .ToListAsync();

        return incidents
            .GroupBy(i => i.EquipmentType!.Value)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new
            {
                EquipmentType = g.Key.ToString(),
                Count = g.Count()
            })
            .Cast<object>()
            .ToList();
    }

    public async Task<IEnumerable<object>> GetTopDepartmentsAsync()
    {
        var incidents = await _incidentRepo.Query()
            .Where(i => !i.IsDeleted && !string.IsNullOrEmpty(i.RequesterSection))
            .ToListAsync();

        return incidents
            .GroupBy(i => i.RequesterSection)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new
            {
                Department = g.Key,
                Count = g.Count()
            })
            .Cast<object>()
            .ToList();
    }

    public async Task<IEnumerable<object>> GetTopSpecialistsAsync()
    {
        var incidents = await _incidentRepo.Query()
            .Where(i => !i.IsDeleted && i.AssignedSpecialistId.HasValue)
            .ToListAsync();

        return incidents
            .GroupBy(i => i.AssignedSpecialistId!.Value)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g =>
            {
                var resolved = g.Count(i =>
                    i.WorkflowStatus == WorkflowStatus.Resolved ||
                    i.WorkflowStatus == WorkflowStatus.Closed);
                var avgResolution = g.Where(i => i.ResolutionTimeMinutes.HasValue && i.ResolutionTimeMinutes > 0)
                    .Select(i => i.ResolutionTimeMinutes!.Value);
                return new
                {
                    SpecialistId = g.Key,
                    TotalAssigned = g.Count(),
                    TotalResolved = resolved,
                    AverageResolutionMinutes = avgResolution.Any() ? Math.Round(avgResolution.Average(), 2) : 0
                };
            })
            .Cast<object>()
            .ToList();
    }

    public async Task<double> GetMttRAsync()
    {
        var incidents = await _incidentRepo.Query()
            .Where(i => !i.IsDeleted && i.ResolutionTimeMinutes.HasValue && i.ResolutionTimeMinutes > 0)
            .ToListAsync();

        return incidents.Any() ? Math.Round(incidents.Average(i => i.ResolutionTimeMinutes!.Value), 2) : 0;
    }

    public async Task<double> GetMttBAsync()
    {
        var problems = await _problemRepo.Query()
            .Where(p => !p.IsDeleted && p.ResolvedDate.HasValue && p.IdentifiedDate.HasValue)
            .ToListAsync();

        if (problems.Count < 2) return 0;

        var sortedDates = problems
            .OrderBy(p => p.IdentifiedDate!.Value)
            .Select(p => p.IdentifiedDate!.Value)
            .ToList();

        var intervals = new List<double>();
        for (var i = 1; i < sortedDates.Count; i++)
        {
            intervals.Add((sortedDates[i] - sortedDates[i - 1]).TotalHours);
        }

        return intervals.Any() ? Math.Round(intervals.Average(), 2) : 0;
    }

    public async Task<double> GetFcrAsync()
    {
        var incidents = await _incidentRepo.Query()
            .Where(i => !i.IsDeleted &&
                (i.WorkflowStatus == WorkflowStatus.Resolved || i.WorkflowStatus == WorkflowStatus.Closed))
            .ToListAsync();

        if (!incidents.Any()) return 0;

        var firstContactResolved = incidents.Count(i =>
            i.EscalationLevel == EscalationLevel.None &&
            i.ResolvedDate.HasValue &&
            i.AcknowledgedDate.HasValue &&
            (i.ResolvedDate.Value - i.AcknowledgedDate.Value).TotalMinutes <= 30);

        return Math.Round((double)firstContactResolved / incidents.Count * 100, 2);
    }

    public async Task<IEnumerable<object>> GetIncidentsByPriorityAsync()
    {
        var incidents = await _incidentRepo.Query().Where(i => !i.IsDeleted).ToListAsync();

        return incidents
            .GroupBy(i => i.Priority)
            .Select(g => new
            {
                Priority = g.Key.ToString(),
                Count = g.Count()
            })
            .Cast<object>()
            .ToList();
    }

    public async Task<IEnumerable<object>> GetIncidentsBySeverityAsync()
    {
        var incidents = await _incidentRepo.Query().Where(i => !i.IsDeleted).ToListAsync();

        return incidents
            .GroupBy(i => i.Severity)
            .Select(g => new
            {
                Severity = g.Key.ToString(),
                Count = g.Count()
            })
            .Cast<object>()
            .ToList();
    }

    public async Task<IEnumerable<object>> GetIncidentsByStatusAsync()
    {
        var incidents = await _incidentRepo.Query().Where(i => !i.IsDeleted).ToListAsync();

        return incidents
            .GroupBy(i => i.WorkflowStatus)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .Cast<object>()
            .ToList();
    }

    public async Task<IEnumerable<object>> GetProblemsByStatusAsync()
    {
        var problems = await _problemRepo.Query().Where(p => !p.IsDeleted).ToListAsync();

        return problems
            .GroupBy(p => p.Status)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .Cast<object>()
            .ToList();
    }

    public async Task<IEnumerable<object>> GetChangesByStatusAsync()
    {
        var changes = await _changeRepo.Query().Where(c => !c.IsDeleted).ToListAsync();

        return changes
            .GroupBy(c => c.Status)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .Cast<object>()
            .ToList();
    }

    public async Task<IEnumerable<object>> GetEquipmentFailureStatsAsync()
    {
        var incidents = await _incidentRepo.Query()
            .Where(i => !i.IsDeleted && i.EquipmentType.HasValue)
            .ToListAsync();

        return incidents
            .GroupBy(i => i.EquipmentType!.Value)
            .Select(g => 
            {
                var resolved = g.Where(i => i.ResolutionTimeMinutes.HasValue && i.ResolutionTimeMinutes > 0);
                return new
                {
                    EquipmentType = g.Key.ToString(),
                    FailureCount = g.Count(),
                    AverageResolutionTimeMinutes = resolved.Any() ? Math.Round(resolved.Average(i => i.ResolutionTimeMinutes!.Value), 2) : 0
                };
            })
            .Cast<object>()
            .ToList();
    }
}
