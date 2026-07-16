namespace dxpmt.Services;
public sealed class EffectConfirmationFormModel
{
    public DateOnly? MeasurementStartOn { get; set; }
    public DateOnly? MeasurementEndOn { get; set; }
    public string Owner { get; set; } = string.Empty;
    public string Baseline { get; set; } = string.Empty;
    public string TargetEffect { get; set; } = string.Empty;
    public string ActualEffect { get; set; } = string.Empty;
    public string MeasurementMethod { get; set; } = string.Empty;
    public string DifferenceAnalysis { get; set; } = string.Empty;
    public string RemainingIssues { get; set; } = string.Empty;
    public string StandardizationPlan { get; set; } = string.Empty;
    public string FollowUpPlan { get; set; } = string.Empty;
}
