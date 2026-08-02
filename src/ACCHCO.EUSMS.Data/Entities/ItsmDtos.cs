namespace ACCHCO.EUSMS.Data.Entities;

public class DashboardKpiDto
{
    public int TotalOpen { get; set; }
    public int TotalResolved { get; set; }
    public int TotalCritical { get; set; }
    public int TotalEscalated { get; set; }
    public int CreatedThisMonth { get; set; }
    public int ResolvedThisMonth { get; set; }
    public double AverageResolutionTimeMinutes { get; set; }
}

public class SlaStatusDto
{
    public double RemainingMinutes { get; set; }
    public double ElapsedMinutes { get; set; }
    public bool IsBreached { get; set; }
    public string? SlaName { get; set; }
    public double ResolutionTimeMinutes { get; set; }
}
