namespace ACCHCO.EUSMS.Data.Entities;

public enum IncidentSeverity { Low = 1, Medium = 2, High = 3, Critical = 4 }
public enum IncidentImpact { SingleUser = 1, Department = 2, MultipleDepartments = 3, EntireOrganization = 4 }
public enum IncidentUrgency { Low = 1, Medium = 2, High = 3 }
public enum IncidentSource { Phone = 1, Email = 2, Portal = 3, WalkIn = 4, Monitoring = 5, Other = 6 }
public enum ChangeType { Standard = 1, Normal = 2, Emergency = 3 }
public enum ChangeRisk { Low = 1, Medium = 2, High = 3, Critical = 4 }
public enum ChangeApprovalStatus { Pending = 1, Approved = 2, Rejected = 3, Cancelled = 4 }
public enum ChangeStatus { Draft = 1, Submitted = 2, Approved = 3, Scheduled = 4, Implementing = 5, Testing = 6, Completed = 7, RolledBack = 8, Cancelled = 9 }
public enum ProblemStatus { Logged = 1, Investigating = 2, RootCauseIdentified = 3, WorkaroundKnown = 4, Resolved = 5, Closed = 6 }
public enum KnownErrorStatus { Active = 1, WorkaroundAvailable = 2, Resolved = 3, Closed = 4 }
public enum CIStatus { Operational = 1, Degraded = 2, OutOfService = 3, Retired = 4 }
public enum ServiceRequestType { SoftwareInstallation = 1, PrinterInstallation = 2, DomainJoin = 3, PasswordReset = 4, PermissionRequest = 5, SharedFolderRequest = 6, NewUserRequest = 7, EmailRequest = 8, HardwareRequest = 9, ApplicationAccess = 10, Other = 11 }
public enum ServiceRequestStatus { New = 1, Submitted = 2, Approved = 3, InProgress = 4, Completed = 5, Cancelled = 6, Rejected = 7 }
public enum ApprovalStatus { Pending = 1, Approved = 2, Rejected = 3 }
public enum WorkflowStatus { New = 1, Assigned = 2, Accepted = 3, InProgress = 4, WaitingUser = 5, WaitingOther = 6, Pending = 7, Resolved = 8, Closed = 9, Cancelled = 10 }
public enum NotificationType { Assignment = 1, Transfer = 2, Escalation = 3, SlaWarning = 4, SlaBreach = 5, NewTicket = 6, ResolvedTicket = 7, ChangeApproval = 8, ProblemUpdate = 9 }
public enum EscalationLevel { None = 1, Level1 = 2, Level2 = 3, Level3 = 4, Management = 5 }
public enum IncidentStatus { New = 1, Assigned = 2, InProgress = 3, Resolved = 4, Closed = 5, Reopened = 6 }
