namespace dxpmt.Domain;

public sealed class GateReview
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string Gate { get; set; } = string.Empty;
    public string Decision { get; set; } = GateDecisions.Pending;
    public string ReviewerName { get; set; } = string.Empty;
    public DateOnly ReviewedOn { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string ChecklistJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }

    public ImprovementCase Case { get; set; } = null!;
}

public static class Gates
{
    public const string G0 = "G0";
    public const string G1 = "G1";
    public const string G2 = "G2";
    public static readonly IReadOnlyList<string> Phase1 = [G0, G1, G2];

    public static string GetName(string gate) => gate switch
    {
        G0 => "G0 調査着手",
        G1 => "G1 範囲確定",
        G2 => "G2 As-Is確定",
        _ => gate
    };
}

public static class GateDecisions
{
    public const string Pending = "判断待ち";
    public const string Approved = "承認";
    public const string Returned = "差戻し";
}
