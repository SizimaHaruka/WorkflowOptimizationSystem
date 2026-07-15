namespace dxpmt.Domain;

public sealed class CaseStatusHistory
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }

    public ImprovementCase Case { get; set; } = null!;
}
