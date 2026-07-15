namespace dxpmt.Domain;

public sealed class TraceLink
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public DateTime CreatedAt { get; set; }
    public ImprovementCase Case { get; set; } = null!;
}

public static class TraceLinkTypes
{
    public const string WorkItem = "WorkItem";
    public const string Problem = "Problem";
}
