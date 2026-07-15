namespace dxpmt.Services;

public sealed class WorkItemFormModel
{
    public string Purpose { get; set; } = string.Empty;
    public string StartTrigger { get; set; } = string.Empty;
    public string PreviousWork { get; set; } = string.Empty;
    public string ReceivedInput { get; set; } = string.Empty;
    public string CompletionCondition { get; set; } = string.Empty;
    public string NextWork { get; set; } = string.Empty;
    public string Handoff { get; set; } = string.Empty;
    public string ActualSteps { get; set; } = string.Empty;
    public string InformationAndAssets { get; set; } = string.Empty;
    public string JudgementAndConfirmation { get; set; } = string.Empty;
    public string ExceptionsAndDifferences { get; set; } = string.Empty;
    public string Workload { get; set; } = string.Empty;
    public string CurrentProblems { get; set; } = string.Empty;
    public string UnconfirmedItems { get; set; } = string.Empty;
    public string ConfirmationNotes { get; set; } = string.Empty;
}

public sealed class AsIsFlowFormModel
{
    public string FlowName { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Reviewer { get; set; } = string.Empty;
    public string KeyJudgements { get; set; } = string.Empty;
    public string KeyExceptions { get; set; } = string.Empty;
    public bool IsConnectedEndToEnd { get; set; }
    public bool AreDepartmentsClear { get; set; }
    public bool AreInputsOutputsConnected { get; set; }
    public bool AreJudgementCriteriaConfirmed { get; set; }
    public bool AreExceptionsIncluded { get; set; }
    public bool IsSeparatedFromToBe { get; set; }
}
