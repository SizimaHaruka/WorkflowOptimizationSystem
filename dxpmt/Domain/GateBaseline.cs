namespace dxpmt.Domain;

public sealed class GateBaseline
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int GateReviewId { get; set; }
    public string Gate { get; set; } = string.Empty;
    public string FormType { get; set; } = string.Empty;
    public int Version { get; set; }
    public string ContentJson { get; set; } = "{}";
    public string ConfirmedBy { get; set; } = string.Empty;
    public DateTime ConfirmedAt { get; set; }
    public ImprovementCase Case { get; set; } = null!;
    public GateReview GateReview { get; set; } = null!;
}
