namespace dxpmt.Domain;

public sealed class CaseForm
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string FormType { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string Status { get; set; } = FormStatuses.Draft;
    public string ContentJson { get; set; } = "{}";
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ImprovementCase Case { get; set; } = null!;
}

public static class FormTypes
{
    public const string Reception = "F01";
    public const string SurveyPlan = "F02";
    public const string AsIsWorkItems = "F03";
    public const string AsIsFlow = "F04";
    public const string Problems = "F05";
    public const string ImprovementOptions = "F06";
    public const string ToBeWorkItems = "F07";
    public const string Requirements = "F08";
    public const string EffectConfirmation = "F09";
}

public static class FormStatuses
{
    public const string Draft = "下書き";
    public const string Confirmed = "確認済み";
}
