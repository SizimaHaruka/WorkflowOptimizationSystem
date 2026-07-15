namespace dxpmt.Services;

public sealed class ReceptionFormModel
{
    public string ProblemToSolve { get; set; } = string.Empty;
    public string OccurrenceSituation { get; set; } = string.Empty;
    public string TargetBusinessAndDepartments { get; set; } = string.Empty;
    public string CurrentResponse { get; set; } = string.Empty;
    public string ExpectedState { get; set; } = string.Empty;
    public string PreferredApproach { get; set; } = string.Empty;
    public ImpactModel Impact { get; set; } = new();
    public string Frequency { get; set; } = string.Empty;
    public string NumberOfCases { get; set; } = string.Empty;
    public string OccurrencePeriod { get; set; } = string.Empty;
    public string ExamplesAndEvidence { get; set; } = string.Empty;
    public string RelatedCasesAndExistingFunctions { get; set; } = string.Empty;
    public string ScopeSummary { get; set; } = string.Empty;
    public string RelatedDepartments { get; set; } = string.Empty;
    public string InitialDecision { get; set; } = "調査着手";
    public string DecisionReason { get; set; } = string.Empty;
    public string NextPerson { get; set; } = string.Empty;
    public DateOnly? NextDueDate { get; set; }
}

public sealed class ImpactModel
{
    public string ManHoursAndCost { get; set; } = string.Empty;
    public string DeliveryAndCapacity { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public string InventoryAndLogistics { get; set; } = string.Empty;
    public string SafetyAndCompliance { get; set; } = string.Empty;
    public string InternalControlAndSecurity { get; set; } = string.Empty;
    public string CustomersAndPartners { get; set; } = string.Empty;
}

public sealed class SurveyPlanFormModel
{
    public string SurveyLead { get; set; } = string.Empty;
    public DateOnly? SurveyStartDate { get; set; }
    public DateOnly? SurveyEndDate { get; set; }
    public string TargetSitesAndDepartments { get; set; } = string.Empty;
    public string StartEvent { get; set; } = string.Empty;
    public string EndStateAndDeliverable { get; set; } = string.Empty;
    public string TargetBusiness { get; set; } = string.Empty;
    public string TargetProductCustomerPeriod { get; set; } = string.Empty;
    public string ExcludedScope { get; set; } = string.Empty;
    public string ExceptionConditions { get; set; } = string.Empty;
    public List<SurveyStakeholderModel> Stakeholders { get; set; } = [];
    public List<SurveyStepModel> SurveySteps { get; set; } = [];
    public List<RequiredMaterialModel> RequiredMaterials { get; set; } = [];

    public void EnsureRows()
    {
        while (Stakeholders.Count < 4)
        {
            Stakeholders.Add(new SurveyStakeholderModel());
        }

        var defaultSteps = new[] { "資料確認", "ヒアリング", "現場観察", "実績分析", "レビュー" };
        while (SurveySteps.Count < defaultSteps.Length)
        {
            SurveySteps.Add(new SurveyStepModel { Sequence = SurveySteps.Count + 1, Method = defaultSteps[SurveySteps.Count] });
        }

        while (RequiredMaterials.Count < 4)
        {
            RequiredMaterials.Add(new RequiredMaterialModel());
        }
    }
}

public sealed class SurveyStakeholderModel
{
    public string DepartmentOrOrganization { get; set; } = string.Empty;
    public string NameAndRole { get; set; } = string.Empty;
    public string ConfirmationTarget { get; set; } = string.Empty;
    public string ParticipationMethod { get; set; } = string.Empty;
}

public sealed class SurveyStepModel
{
    public int Sequence { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateOnly? PlannedOn { get; set; }
    public bool IsCompleted { get; set; }
}

public sealed class RequiredMaterialModel
{
    public string Name { get; set; } = string.Empty;
    public string OwningDepartment { get; set; } = string.Empty;
    public DateOnly? RequestedOn { get; set; }
    public DateOnly? ReceivedOn { get; set; }
    public string Notes { get; set; } = string.Empty;
}
