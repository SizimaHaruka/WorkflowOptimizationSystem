using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Cases;

public sealed class GateModel(ApplicationDbContext database, CurrentUserService currentUser) : PageModel
{
    [BindProperty]
    public GateReviewInput Input { get; set; } = new();

    public ImprovementCase Case { get; private set; } = null!;
    public string Gate { get; private set; } = string.Empty;
    public string GateName => Gates.GetName(Gate);
    public string ReviewerName => currentUser.DisplayName;

    public async Task<IActionResult> OnGetAsync(int caseId, string gate)
    {
        if (!Gates.All.Contains(gate) || !await LoadCaseAsync(caseId)) return NotFound();
        Gate = gate;
        Input.ReviewedOn = DateOnly.FromDateTime(DateTime.Today);
        Input.ReviewerName = ReviewerName;
        Input.Decision = GateDecisions.Approved;
        await SetSuggestedChecklistAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int caseId, string gate)
    {
        if (!Gates.All.Contains(gate) || !await LoadCaseAsync(caseId)) return NotFound();
        Gate = gate;
        Input.ReviewerName = ReviewerName;
        ModelState.Remove("Input.ReviewerName");
        if (Input.Decision is not GateDecisions.Approved and not GateDecisions.Returned)
        {
            ModelState.AddModelError("Input.Decision", "承認または差戻しを選択してください。");
        }

        if (Input.Decision == GateDecisions.Approved && !Input.IsChecklistComplete(gate))
        {
            ModelState.AddModelError(string.Empty, "承認するには、すべての完了条件を確認してください。");
        }

        if (gate != Gates.G0 && Input.Decision == GateDecisions.Approved)
        {
            var priorGate = Gates.All[Gates.All.ToList().IndexOf(gate) - 1];
            var priorApproved = await database.GateReviews.AnyAsync(x => x.CaseId == caseId && x.Gate == priorGate && x.Decision == GateDecisions.Approved);
            if (!priorApproved)
            {
                ModelState.AddModelError(string.Empty, $"{gate} を承認する前に、{priorGate} の承認を完了してください。");
            }
        }

        if (!ModelState.IsValid) return Page();

        var now = DateTime.UtcNow;
        database.GateReviews.Add(new GateReview
        {
            CaseId = caseId,
            Gate = gate,
            Decision = Input.Decision,
            ReviewerName = ReviewerName,
            ReviewedOn = Input.ReviewedOn,
            Comment = Input.Comment?.Trim() ?? string.Empty,
            ChecklistJson = JsonSerializer.Serialize(Input.ChecklistFor(gate)),
            CreatedAt = now
        });

        if (Input.Decision is GateDecisions.Approved or GateDecisions.Returned)
        {
            await CaptureFormVersionsAsync(caseId, now);
        }

        if (Input.Decision == GateDecisions.Returned)
        {
            var returnedGateIndex = Gates.All.ToList().IndexOf(gate);
            foreach (var laterGate in Gates.All.Skip(returnedGateIndex + 1))
            {
                var latest = await database.GateReviews.Where(x => x.CaseId == caseId && x.Gate == laterGate)
                    .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
                if (latest?.Decision == GateDecisions.Approved)
                {
                    database.GateReviews.Add(new GateReview { CaseId = caseId, Gate = laterGate, Decision = GateDecisions.Returned,
                        ReviewerName = ReviewerName, ReviewedOn = Input.ReviewedOn,
                        Comment = $"{gate} の差戻しに伴う自動差戻し。", ChecklistJson = "{}", CreatedAt = now });
                }
            }
        }

        var targetStatus = Input.Decision switch
        {
            GateDecisions.Approved when gate == Gates.G0 => CaseStatuses.Investigating,
            GateDecisions.Approved when gate == Gates.G1 => CaseStatuses.AsIsReview,
            GateDecisions.Approved when gate == Gates.G2 => CaseStatuses.ImprovementReview,
            GateDecisions.Approved when gate == Gates.G3 => CaseStatuses.ToBeReview,
            GateDecisions.Approved when gate == Gates.G4 => CaseStatuses.ImplementationDecision,
            GateDecisions.Approved when gate == Gates.G5 => CaseStatuses.Implementing,
            GateDecisions.Approved when gate == Gates.G6 => CaseStatuses.Completed,
            GateDecisions.Returned => CaseStatuses.Returned,
            _ => Case.Status
        };
        if (targetStatus != Case.Status)
        {
            database.CaseStatusHistories.Add(new CaseStatusHistory
            {
                CaseId = caseId,
                PreviousStatus = Case.Status,
                NewStatus = targetStatus,
                ChangedBy = ReviewerName,
                Comment = $"{GateName}: {Input.Decision}。{Input.Comment?.Trim()}",
                ChangedAt = now
            });
            Case.Status = targetStatus;
        }
        Case.UpdatedAt = now;
        await database.SaveChangesAsync();

        TempData["SuccessMessage"] = $"{GateName} の判定を記録しました。";
        return RedirectToPage("/Cases/Details", new { id = caseId });
    }

    private async Task<bool> LoadCaseAsync(int caseId)
    {
        var item = await database.Cases.SingleOrDefaultAsync(x => x.Id == caseId);
        if (item is null) return false;
        Case = item;
        return true;
    }

    private async Task CaptureFormVersionsAsync(int caseId, DateTime now)
    {
        var currentForms = await database.CaseForms
            .Where(x => x.CaseId == caseId)
            .GroupBy(x => x.FormType)
            .Select(group => group.OrderByDescending(x => x.Version).First())
            .ToListAsync();

        foreach (var form in currentForms)
        {
            form.Status = FormStatuses.Confirmed;
            database.CaseForms.Add(new CaseForm
            {
                CaseId = form.CaseId,
                FormType = form.FormType,
                Version = form.Version + 1,
                Status = FormStatuses.Draft,
                ContentJson = form.ContentJson,
                UpdatedAt = now
            });
        }
    }

    private async Task SetSuggestedChecklistAsync()
    {
        if (Gate == Gates.G0)
        {
            var form = await database.CaseForms.Where(x => x.CaseId == Case.Id && x.FormType == FormTypes.Reception).OrderByDescending(x => x.Version).FirstOrDefaultAsync();
            var content = Deserialize<ReceptionFormModel>(form?.ContentJson);
            Input.ProblemIsClear = !string.IsNullOrWhiteSpace(content?.ProblemToSolve);
            Input.RequesterIsClear = !string.IsNullOrWhiteSpace(Case.RequesterName);
            Input.OwnerIsClear = !string.IsNullOrWhiteSpace(Case.OwnerName);
            Input.ScopeIsClear = !string.IsNullOrWhiteSpace(content?.ScopeSummary) || !string.IsNullOrWhiteSpace(Case.ScopeSummary);
        }
        else if (Gate == Gates.G1)
        {
            var form = await database.CaseForms.Where(x => x.CaseId == Case.Id && x.FormType == FormTypes.SurveyPlan).OrderByDescending(x => x.Version).FirstOrDefaultAsync();
            var content = Deserialize<SurveyPlanFormModel>(form?.ContentJson);
            Input.StartEventIsClear = !string.IsNullOrWhiteSpace(content?.StartEvent);
            Input.EndStateIsClear = !string.IsNullOrWhiteSpace(content?.EndStateAndDeliverable);
            Input.TargetDepartmentsAreAgreed = !string.IsNullOrWhiteSpace(content?.TargetSitesAndDepartments);
            Input.ExcludedScopeIsAgreed = !string.IsNullOrWhiteSpace(content?.ExcludedScope);
        }
        else if (Gate == Gates.G2)
        {
            var flow = await database.CaseForms.Where(x => x.CaseId == Case.Id && x.FormType == FormTypes.AsIsFlow).OrderByDescending(x => x.Version).FirstOrDefaultAsync();
            var content = Deserialize<AsIsFlowFormModel>(flow?.ContentJson);
            var workItems = await database.WorkItems.Where(x => x.CaseId == Case.Id && x.WorkType == WorkItemTypes.AsIs && !x.IsDeleted).ToListAsync();
            Input.WorkItemsAreConfirmed = workItems.Count > 0 && workItems.All(x => x.IsConfirmed);
            Input.FlowIsConnected = content?.IsConnectedEndToEnd == true;
            Input.InputsOutputsAreConfirmed = content?.AreInputsOutputsConnected == true;
            Input.JudgementsAreConfirmed = content?.AreJudgementCriteriaConfirmed == true;
            Input.ExceptionsAreIncluded = content?.AreExceptionsIncluded == true;
        }
        else
        {
            var options = await database.ImprovementOptions.CountAsync(x => x.CaseId == Case.Id);
            var toBe = await database.WorkItems.CountAsync(x => x.CaseId == Case.Id && x.WorkType == WorkItemTypes.ToBe && !x.IsDeleted);
            var requirements = await database.Requirements.CountAsync(x => x.CaseId == Case.Id);
            var effectFormExists = await database.CaseForms.AnyAsync(x => x.CaseId == Case.Id && x.FormType == FormTypes.EffectConfirmation);
            Input.WorkItemsAreConfirmed = Gate == Gates.G3 ? options > 0 : Gate == Gates.G6 ? effectFormExists : toBe > 0;
            Input.FlowIsConnected = Gate is Gates.G5 or Gates.G6 ? requirements > 0 : true;
            Input.InputsOutputsAreConfirmed = true;
            Input.JudgementsAreConfirmed = true;
            Input.ExceptionsAreIncluded = true;
        }
    }

    private static T? Deserialize<T>(string? contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson)) return default;
        try { return JsonSerializer.Deserialize<T>(contentJson); }
        catch (JsonException) { return default; }
    }

    public sealed class GateReviewInput
    {
        [Required(ErrorMessage = "判定者を入力してください。"), StringLength(100), Display(Name = "判定者")]
        public string ReviewerName { get; set; } = string.Empty;
        [Display(Name = "判断日")]
        public DateOnly ReviewedOn { get; set; }
        [Required, Display(Name = "判定")]
        public string Decision { get; set; } = GateDecisions.Approved;
        [StringLength(2000), Display(Name = "コメント")]
        public string? Comment { get; set; }
        public bool ProblemIsClear { get; set; }
        public bool RequesterIsClear { get; set; }
        public bool OwnerIsClear { get; set; }
        public bool ScopeIsClear { get; set; }
        public bool StartEventIsClear { get; set; }
        public bool EndStateIsClear { get; set; }
        public bool TargetDepartmentsAreAgreed { get; set; }
        public bool ExcludedScopeIsAgreed { get; set; }
        public bool WorkItemsAreConfirmed { get; set; }
        public bool FlowIsConnected { get; set; }
        public bool InputsOutputsAreConfirmed { get; set; }
        public bool JudgementsAreConfirmed { get; set; }
        public bool ExceptionsAreIncluded { get; set; }

        public bool IsChecklistComplete(string gate) => gate switch
        {
            Gates.G0 => ProblemIsClear && RequesterIsClear && OwnerIsClear && ScopeIsClear,
            Gates.G1 => StartEventIsClear && EndStateIsClear && TargetDepartmentsAreAgreed && ExcludedScopeIsAgreed,
            Gates.G2 => WorkItemsAreConfirmed && FlowIsConnected && InputsOutputsAreConfirmed && JudgementsAreConfirmed && ExceptionsAreIncluded,
            _ => false
        };

        public object ChecklistFor(string gate) => gate switch
        {
            Gates.G0 => new { ProblemIsClear, RequesterIsClear, OwnerIsClear, ScopeIsClear },
            Gates.G1 => new { StartEventIsClear, EndStateIsClear, TargetDepartmentsAreAgreed, ExcludedScopeIsAgreed },
            _ => new { WorkItemsAreConfirmed, FlowIsConnected, InputsOutputsAreConfirmed, JudgementsAreConfirmed, ExceptionsAreIncluded }
        };
    }
}
