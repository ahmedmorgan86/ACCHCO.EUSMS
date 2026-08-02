using Microsoft.EntityFrameworkCore;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;

namespace ACCHCO.EUSMS.Services.Interfaces;

public interface IIncidentService
{
    Task<Incident?> GetByIdAsync(int id);
    Task<Incident?> GetByNumberAsync(string incidentNumber);
    Task<Incident?> GetWithDetailsAsync(int id);
    Task<IEnumerable<Incident>> GetAllAsync();
    Task<IEnumerable<Incident>> SearchAsync(string query);
    Task<Incident> CreateAsync(Incident incident, string currentUser);
    Task<Incident> UpdateAsync(Incident incident, string currentUser);
    Task<bool> DeleteAsync(int incidentId, string currentUser);
    Task<bool> AssignAsync(int incidentId, int specialistId, string currentUser);
    Task<bool> EscalateAsync(int incidentId, EscalationLevel level, string currentUser);
    Task<bool> ResolveAsync(int incidentId, string resolutionNotes, string currentUser);
    Task<bool> CloseAsync(int incidentId, string closureNotes, string closureCode, bool customerSatisfied, string currentUser);
    Task<bool> ReopenAsync(int incidentId, string currentUser);
    Task<IncidentComment> AddCommentAsync(int incidentId, string comment, int? authorId, string authorName, bool isInternal);
    Task<IEnumerable<IncidentComment>> GetCommentsAsync(int incidentId);
    Task<IEnumerable<IncidentHistory>> GetHistoryAsync(int incidentId);
    Task<IEnumerable<Incident>> GetFilteredAsync(DateTime? from, DateTime? to, Shift? shift,
        int? assignedSpecialistId, string? section, EquipmentType? equipmentType,
        Priority? priority, IncidentSeverity? severity, IncidentStatus? status,
        EscalationLevel? escalationLevel, int? requesterId);
    Task<DashboardKpiDto> GetDashboardKpisAsync();
    Task<bool> CheckDuplicateAsync(int? equipmentTypeId, string? equipmentName, string? description, int? excludeIncidentId);
    Task AutoEscalateAsync();
}

public interface IChangeRequestService
{
    Task<ChangeRequest?> GetByIdAsync(int id);
    Task<ChangeRequest?> GetByNumberAsync(string changeNumber);
    Task<IEnumerable<ChangeRequest>> GetAllAsync();
    Task<ChangeRequest> CreateAsync(ChangeRequest changeRequest, string currentUser);
    Task<ChangeRequest> UpdateAsync(ChangeRequest changeRequest, string currentUser);
    Task<bool> DeleteAsync(int changeRequestId, string currentUser);
    Task<bool> SubmitAsync(int changeRequestId, string currentUser);
    Task<bool> ApproveAsync(int changeRequestId, string approverName, string? notes, string currentUser);
    Task<bool> RejectAsync(int changeRequestId, string approverName, string? notes, string currentUser);
    Task<bool> StartImplementationAsync(int changeRequestId, string currentUser);
    Task<bool> CompleteAsync(int changeRequestId, string? implementationNotes, string? testingResult, string currentUser);
    Task<bool> RollbackAsync(int changeRequestId, string currentUser);
    Task<IEnumerable<ChangeRequest>> GetFilteredAsync(ChangeType? changeType, ChangeStatus? status,
        ChangeRisk? riskLevel, ChangeApprovalStatus? approvalStatus, DateTime? from, DateTime? to);
    Task<IEnumerable<ChangeRequest>> GetPendingApprovalAsync();
}

public interface IProblemService
{
    Task<Problem?> GetByIdAsync(int id);
    Task<Problem?> GetByNumberAsync(string problemNumber);
    Task<IEnumerable<Problem>> GetAllAsync();
    Task<Problem> CreateAsync(Problem problem, string currentUser);
    Task<Problem> UpdateAsync(Problem problem, string currentUser);
    Task<bool> DeleteAsync(int problemId, string currentUser);
    Task<bool> LinkIncidentAsync(int problemId, int incidentId, string? notes);
    Task<bool> UnlinkIncidentAsync(int problemId, int incidentId);
    Task<IEnumerable<Incident>> GetLinkedIncidentsAsync(int problemId);
    Task<bool> IdentifyRootCauseAsync(int problemId, string rootCause, string currentUser);
    Task<bool> SetWorkaroundAsync(int problemId, string workaround, string currentUser);
    Task<bool> SetPermanentSolutionAsync(int problemId, string permanentSolution, string currentUser);
    Task<bool> ResolveAsync(int problemId, string resolutionNotes, string currentUser);
    Task<bool> CloseAsync(int problemId, string currentUser);
}

public interface IKnownErrorService
{
    Task<KnownError?> GetByIdAsync(int id);
    Task<KnownError?> GetByNumberAsync(string knownErrorNumber);
    Task<IEnumerable<KnownError>> GetAllAsync();
    Task<KnownError> CreateAsync(KnownError knownError, string currentUser);
    Task<KnownError> UpdateAsync(KnownError knownError, string currentUser);
    Task<bool> DeleteAsync(int knownErrorId, string currentUser);
    Task<IEnumerable<KnownError>> SearchAsync(string query);
    Task<IEnumerable<KnownError>> GetByStatusAsync(KnownErrorStatus status);
}

public interface IConfigurationItemService
{
    Task<ConfigurationItem?> GetByIdAsync(int id);
    Task<ConfigurationItem?> GetByNumberAsync(string ciNumber);
    Task<IEnumerable<ConfigurationItem>> GetAllAsync();
    Task<ConfigurationItem> CreateAsync(ConfigurationItem ci, string currentUser);
    Task<ConfigurationItem> UpdateAsync(ConfigurationItem ci, string currentUser);
    Task<bool> DeleteAsync(int ciId, string currentUser);
    Task<IEnumerable<ConfigurationItem>> SearchAsync(string query);
    Task<IEnumerable<ConfigurationItem>> GetByTypeAsync(EquipmentType equipmentType);
    Task<IEnumerable<ConfigurationItem>> GetByDepartmentAsync(string department);
    Task<IEnumerable<Incident>> GetIncidentsAsync(int ciId);
    Task<IEnumerable<ChangeRequest>> GetChangesAsync(int ciId);
}

public interface IServiceRequestService
{
    Task<ServiceRequest?> GetByIdAsync(int id);
    Task<ServiceRequest?> GetByNumberAsync(string requestNumber);
    Task<IEnumerable<ServiceRequest>> GetAllAsync();
    Task<ServiceRequest> CreateAsync(ServiceRequest serviceRequest, string currentUser);
    Task<ServiceRequest> UpdateAsync(ServiceRequest serviceRequest, string currentUser);
    Task<bool> DeleteAsync(int serviceRequestId, string currentUser);
    Task<bool> SubmitAsync(int serviceRequestId, string currentUser);
    Task<bool> ApproveAsync(int serviceRequestId, string approverName, string? notes, string currentUser);
    Task<bool> RejectAsync(int serviceRequestId, string approverName, string? notes, string currentUser);
    Task<bool> StartAsync(int serviceRequestId, string currentUser);
    Task<bool> CompleteAsync(int serviceRequestId, string? completionNotes, string currentUser);
    Task<bool> CancelAsync(int serviceRequestId, string currentUser);
    Task<IEnumerable<ServiceRequest>> GetFilteredAsync(ServiceRequestType? type, ServiceRequestStatus? status,
        ApprovalStatus? approvalStatus, DateTime? from, DateTime? to, string? section);
    Task<IEnumerable<ServiceRequest>> GetPendingApprovalAsync();
}

public interface ISlaService
{
    Task<SlaDefinition?> GetByIdAsync(int id);
    Task<IEnumerable<SlaDefinition>> GetAllAsync();
    Task<SlaDefinition> CreateAsync(SlaDefinition sla, string currentUser);
    Task<SlaDefinition> UpdateAsync(SlaDefinition sla, string currentUser);
    Task<bool> DeleteAsync(int slaId, string currentUser);
    Task<(double remainingMinutes, bool isBreached)> CalculateRemainingTimeAsync(int incidentId);
    Task<bool> CheckSlaComplianceAsync(int incidentId);
    Task<SlaStatusDto> GetSlaStatusAsync(int incidentId);
    Task<SlaDefinition?> MatchSlaDefinitionAsync(Priority priority, string? category,
        EquipmentType? equipmentType, string? department);
}

public interface IEscalationService
{
    Task<EscalationRule?> GetRuleByIdAsync(int id);
    Task<IEnumerable<EscalationRule>> GetAllRulesAsync();
    Task<EscalationRule> CreateRuleAsync(EscalationRule rule, string currentUser);
    Task<EscalationRule> UpdateRuleAsync(EscalationRule rule, string currentUser);
    Task<bool> DeleteRuleAsync(int ruleId, string currentUser);
    Task<int> ProcessEscalationsAsync();
    Task<bool> EscalateIncidentAsync(int incidentId, EscalationLevel targetLevel, string currentUser);
}

public interface INotificationService
{
    Task<Notification> CreateAsync(Notification notification, string currentUser);
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int notificationId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task SendAssignmentNotificationAsync(int userId, string userName, string entityType, int entityId, string entityNumber);
    Task SendEscalationNotificationAsync(int userId, string userName, string entityType, int entityId, string entityNumber, EscalationLevel level);
    Task SendSlaWarningAsync(int userId, string userName, string entityType, int entityId, string entityNumber, double remainingMinutes);
    Task SendSlaBreachAsync(int userId, string userName, string entityType, int entityId, string entityNumber);
}

public interface IItsmAnalyticsService
{
    Task<IEnumerable<object>> GetIncidentTrendAsync();
    Task<IEnumerable<object>> GetTopEquipmentFailuresAsync();
    Task<IEnumerable<object>> GetTopDepartmentsAsync();
    Task<IEnumerable<object>> GetTopSpecialistsAsync();
    Task<double> GetMttRAsync();
    Task<double> GetMttBAsync();
    Task<double> GetFcrAsync();
    Task<IEnumerable<object>> GetIncidentsByPriorityAsync();
    Task<IEnumerable<object>> GetIncidentsBySeverityAsync();
    Task<IEnumerable<object>> GetIncidentsByStatusAsync();
    Task<IEnumerable<object>> GetProblemsByStatusAsync();
    Task<IEnumerable<object>> GetChangesByStatusAsync();
    Task<IEnumerable<object>> GetEquipmentFailureStatsAsync();
}
