namespace dxpmt.Domain;

public sealed class Problem
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int? WorkItemId { get; set; }
    public int Sequence { get; set; }
    public string Category { get; set; } = ProblemCategories.Other;
    public string Phenomenon { get; set; } = string.Empty;
    public string OccurrenceCondition { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string CurrentWorkaround { get; set; } = string.Empty;
    public string CauseHypothesis { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string Severity { get; set; } = CasePriorities.Medium;
    public string Status { get; set; } = ProblemStatuses.Confirming;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ImprovementCase Case { get; set; } = null!;
    public WorkItem? WorkItem { get; set; }
}

public static class ProblemCategories
{
    public static readonly IReadOnlyList<string> All = ["不要", "重複", "転記", "待ち", "手戻り", "属人化", "非標準", "情報不足", "過剰品質", "システム制約", "統制不足", "例外過多", "その他"];
    public const string Other = "その他";
}

public static class ProblemStatuses
{
    public const string Confirming = "確認中";
    public const string Confirmed = "確定";
    public static readonly IReadOnlyList<string> All = [Confirming, Confirmed];
}
