namespace ACCHCO.EUSMS.Data.Entities;

public class ShiftHandover : BaseEntity
{
    public DateTime HandoverDate { get; set; } = DateTime.Today;
    public Shift FromShift { get; set; } = Shift.Red;
    public Shift ToShift { get; set; } = Shift.Yellow;
    public int? FromSpecialistId { get; set; }
    public int? ToSpecialistId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? PendingTasks { get; set; }
    public string? ImportantNotes { get; set; }
    public string? EscalationItems { get; set; }
    public int TotalTicketsHandled { get; set; }
    public int PendingTicketCount { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedDate { get; set; }

    public AppUser? FromSpecialist { get; set; }
    public AppUser? ToSpecialist { get; set; }
    public ICollection<ShiftHandoverTicket> HandoverTickets { get; set; } = new List<ShiftHandoverTicket>();
}

public class ShiftHandoverTicket : BaseEntity
{
    public int ShiftHandoverId { get; set; }
    public int TicketId { get; set; }
    public string? HandoverNotes { get; set; }
    public bool IsPending { get; set; }

    public ShiftHandover ShiftHandover { get; set; } = null!;
    public Ticket Ticket { get; set; } = null!;
}
