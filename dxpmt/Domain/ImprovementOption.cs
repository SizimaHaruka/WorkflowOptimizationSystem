namespace dxpmt.Domain;

public sealed class ImprovementOption
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int? ProblemId { get; set; }
    public int Sequence { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Approach { get; set; } = string.Empty;
    public string ExpectedEffect { get; set; } = string.Empty;
    public string CostAndEffort { get; set; } = string.Empty;
    public string RisksAndConstraints { get; set; } = string.Empty;
    public string Recommendation { get; set; } = ImprovementRecommendations.ToBeEvaluated;
    public string Status { get; set; } = ImprovementOptionStatuses.Draft;
    public string Evidence { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ImprovementCase Case { get; set; } = null!;
    public Problem? Problem { get; set; }
}

public static class ImprovementRecommendations
{
    public const string Recommended = "推奨";
    public const string Possible = "条件付き";
    public const string NotRecommended = "非推奨";
    public const string ToBeEvaluated = "評価中";
    public static readonly IReadOnlyList<string> All = [Recommended, Possible, NotRecommended, ToBeEvaluated];
}

public static class ImprovementOptionStatuses
{
    public const string Draft = "検討中";
    public const string Selected = "採用";
    public const string Rejected = "見送り";
    public static readonly IReadOnlyList<string> All = [Draft, Selected, Rejected];
}
