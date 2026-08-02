namespace ACCHCO.EUSMS.Data.Entities;

public enum UserRole
{
    SystemAdministrator = 1,
    DepartmentManager = 2,
    SupportSpecialist = 3,
    ReadOnly = 4
}

public enum Shift
{
    Red = 1,
    Yellow = 2,
    Blue = 3
}

public enum TicketStatus
{
    Open = 1,
    InProgress = 2,
    OnHold = 3,
    Resolved = 4,
    Closed = 5
}

public enum Priority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum EquipmentType
{
    RTG = 1,
    Kalmar = 2,
    Hyster = 3,
    ReachStacker = 4,
    AccessPoint = 5,
    PC = 6,
    Printer = 7,
    GatesOcrCamera = 8,
    RDT = 9
}

public enum FaultType
{
    Hardware = 1,
    Software = 2,
    Network = 3,
    Connectivity = 4,
    Performance = 5,
    Security = 6,
    Configuration = 7,
    UserError = 8,
    Other = 9
}
