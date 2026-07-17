namespace dxpmt.Domain;

public sealed class WorkItem
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int Sequence { get; set; }
    public string WorkType { get; set; } = WorkItemTypes.AsIs;
    public string BusinessProcessName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DepartmentAndRole { get; set; } = string.Empty;
    public string Performer { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string ContentJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ImprovementCase Case { get; set; } = null!;
    public ICollection<Problem> Problems { get; set; } = [];
}

public static class WorkItemTypes
{
    public const string AsIs = "AsIs";
    public const string ToBe = "ToBe";
}
