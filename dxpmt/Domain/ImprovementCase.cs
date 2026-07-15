namespace dxpmt.Domain;

public sealed class ImprovementCase
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string RequestingDepartment { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateOnly RegisteredOn { get; set; }
    public DateOnly? DesiredBy { get; set; }
    public string Priority { get; set; } = CasePriorities.Medium;
    public string Status { get; set; } = CaseStatuses.Received;
    public string ScopeSummary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<CaseForm> Forms { get; set; } = [];
    public ICollection<ActionItem> ActionItems { get; set; } = [];
    public ICollection<DecisionRecord> DecisionRecords { get; set; } = [];
    public ICollection<GateReview> GateReviews { get; set; } = [];
    public ICollection<CaseStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<WorkItem> WorkItems { get; set; } = [];
    public ICollection<Problem> Problems { get; set; } = [];
    public ICollection<TraceLink> TraceLinks { get; set; } = [];
}

public static class CaseStatuses
{
    public const string Received = "受付";
    public const string PreCheck = "事前確認中";
    public const string Investigating = "調査中";
    public const string AsIsReview = "As-Is確認中";
    public const string ImprovementReview = "改善案検討中";
    public const string Evaluating = "評価中";
    public const string ToBeReview = "To-Be検討及び確認中";
    public const string ImplementationDecision = "実施判断待ち";
    public const string Implementing = "実施中";
    public const string EffectReview = "効果確認中";
    public const string Completed = "完了";
    public const string OnHold = "保留";
    public const string Cancelled = "中止";
    public const string Returned = "差戻し";

    public static readonly IReadOnlyList<string> All =
    [
        Received, PreCheck, Investigating, AsIsReview, ImprovementReview,
        Evaluating, ToBeReview, ImplementationDecision, Implementing,
        EffectReview, Completed, OnHold, Cancelled, Returned
    ];
}

public static class CasePriorities
{
    public const string High = "高";
    public const string Medium = "中";
    public const string Low = "低";

    public static readonly IReadOnlyList<string> All = [High, Medium, Low];
}
