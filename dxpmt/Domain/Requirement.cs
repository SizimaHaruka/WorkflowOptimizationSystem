namespace dxpmt.Domain;
public sealed class Requirement
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int? WorkItemId { get; set; }
    public int Sequence { get; set; }
    public string Category { get; set; } = "業務";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = CasePriorities.Medium;
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public string ImplementationApproach { get; set; } = string.Empty;
    public string Status { get; set; } = "検討中";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ImprovementCase Case { get; set; } = null!;
    public WorkItem? WorkItem { get; set; }
}
public static class RequirementCategories { public static readonly IReadOnlyList<string> All = ["業務", "機能", "データ", "帳票", "連携", "非機能", "統制"]; }
public static class RequirementStatuses { public static readonly IReadOnlyList<string> All = ["検討中", "合意済み", "見送り"]; }
