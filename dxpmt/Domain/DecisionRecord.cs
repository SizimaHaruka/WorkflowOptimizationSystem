namespace dxpmt.Domain;

public sealed class DecisionRecord
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int Sequence { get; set; }
    public DateOnly DecidedOn { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DeciderName { get; set; } = string.Empty;
    public string Participants { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public DateOnly? EffectiveOn { get; set; }
    public string Constraints { get; set; } = string.Empty;
    public string BackgroundAndProblem { get; set; } = string.Empty;
    public string Alternatives { get; set; } = string.Empty;
    public string ReasonForDecision { get; set; } = string.Empty;
    public string RejectedAlternatives { get; set; } = string.Empty;
    public string References { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string FollowUpActions { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public string ReviewCondition { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string ConfirmedBy { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ImprovementCase Case { get; set; } = null!;
}
