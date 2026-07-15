namespace dxpmt.Domain;

public sealed class ActionItem
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int Sequence { get; set; }
    public DateOnly RaisedOn { get; set; }
    public string Category { get; set; } = ActionItemCategories.Unconfirmed;
    public string Content { get; set; } = string.Empty;
    public string ContactOrResponseTarget { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = ActionItemStatuses.NotStarted;
    public string Response { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ImprovementCase Case { get; set; } = null!;
}

public static class ActionItemCategories
{
    public const string Unconfirmed = "未確認事項";
    public const string MaterialRequest = "資料依頼";
    public const string Disagreement = "意見不一致";
    public const string DecisionPending = "判断待ち";
    public const string TechnicalReview = "技術確認";
    public const string ScopeChange = "範囲変更";
    public const string Risk = "リスク";
    public const string Other = "その他";

    public static readonly IReadOnlyList<string> All =
    [Unconfirmed, MaterialRequest, Disagreement, DecisionPending, TechnicalReview, ScopeChange, Risk, Other];
}

public static class ActionItemStatuses
{
    public const string NotStarted = "未着手";
    public const string InProgress = "対応中";
    public const string Completed = "完了";

    public static readonly IReadOnlyList<string> All = [NotStarted, InProgress, Completed];
}
