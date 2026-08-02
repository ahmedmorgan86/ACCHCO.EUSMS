namespace ACCHCO.EUSMS.Data.Entities;

public class IncidentComment : BaseEntity
{
    public int IncidentId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public int? AuthorId { get; set; }
    public bool IsInternal { get; set; }
    public DateTime CommentDate { get; set; }
    public Incident? Incident { get; set; }
    public AppUser? Author { get; set; }
}
