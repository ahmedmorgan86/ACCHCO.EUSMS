namespace ACCHCO.EUSMS.Data.Entities;

public class ProblemIncident : BaseEntity
{
    public int ProblemId { get; set; }
    public int IncidentId { get; set; }
    public string? Notes { get; set; }
    public Problem? Problem { get; set; }
    public Incident? Incident { get; set; }
}
