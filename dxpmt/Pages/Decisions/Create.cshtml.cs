using System.ComponentModel.DataAnnotations;
using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Decisions;

public sealed class CreateModel(ApplicationDbContext database) : PageModel
{
    [BindProperty]
    public DecisionInput Input { get; set; } = new();

    public ImprovementCase Case { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int caseId)
    {
        if (!await LoadCaseAsync(caseId)) return NotFound();
        Input.DecidedOn = DateOnly.FromDateTime(DateTime.Today);
        Input.DeciderName = Case.OwnerName;
        Input.CreatedBy = Case.OwnerName;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int caseId)
    {
        if (!await LoadCaseAsync(caseId)) return NotFound();
        if (!ModelState.IsValid) return Page();

        var sequence = (await database.DecisionRecords.Where(x => x.CaseId == caseId).MaxAsync(x => (int?)x.Sequence) ?? 0) + 1;
        var now = DateTime.UtcNow;
        database.DecisionRecords.Add(new DecisionRecord
        {
            CaseId = caseId, Sequence = sequence, DecidedOn = Input.DecidedOn, Title = Input.Title.Trim(), DeciderName = Input.DeciderName.Trim(),
            Participants = Input.Participants?.Trim() ?? string.Empty, Decision = Input.Decision.Trim(), Scope = Input.Scope?.Trim() ?? string.Empty,
            EffectiveOn = Input.EffectiveOn, Constraints = Input.Constraints?.Trim() ?? string.Empty, BackgroundAndProblem = Input.BackgroundAndProblem?.Trim() ?? string.Empty,
            Alternatives = Input.Alternatives?.Trim() ?? string.Empty, ReasonForDecision = Input.ReasonForDecision?.Trim() ?? string.Empty,
            RejectedAlternatives = Input.RejectedAlternatives?.Trim() ?? string.Empty, References = Input.References?.Trim() ?? string.Empty,
            Impact = Input.Impact?.Trim() ?? string.Empty, FollowUpActions = Input.FollowUpActions?.Trim() ?? string.Empty,
            OwnerName = Input.OwnerName?.Trim() ?? string.Empty, DueDate = Input.DueDate, ReviewCondition = Input.ReviewCondition?.Trim() ?? string.Empty,
            CreatedBy = Input.CreatedBy?.Trim() ?? string.Empty, ConfirmedBy = Input.ConfirmedBy?.Trim() ?? string.Empty, ApprovedBy = Input.ApprovedBy?.Trim() ?? string.Empty,
            CreatedAt = now
        });
        Case.UpdatedAt = now;
        await database.SaveChangesAsync();
        TempData["SuccessMessage"] = "意思決定記録を追加しました。";
        return RedirectToPage("/Decisions/Index", new { caseId });
    }

    private async Task<bool> LoadCaseAsync(int caseId)
    {
        var item = await database.Cases.SingleOrDefaultAsync(x => x.Id == caseId);
        if (item is null) return false;
        Case = item;
        return true;
    }

    public sealed class DecisionInput
    {
        [Display(Name = "決定日")] public DateOnly DecidedOn { get; set; }
        [Required(ErrorMessage = "件名を入力してください。"), StringLength(200), Display(Name = "件名")] public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "決定者を入力してください。"), StringLength(100), Display(Name = "決定者")] public string DeciderName { get; set; } = string.Empty;
        [Display(Name = "参加者")] public string? Participants { get; set; }
        [Required(ErrorMessage = "決定事項を入力してください。"), Display(Name = "決定事項")] public string Decision { get; set; } = string.Empty;
        [Display(Name = "対象範囲")] public string? Scope { get; set; }
        [Display(Name = "適用日")] public DateOnly? EffectiveOn { get; set; }
        [Display(Name = "条件・制約")] public string? Constraints { get; set; }
        [Display(Name = "背景・問題")] public string? BackgroundAndProblem { get; set; }
        [Display(Name = "検討した選択肢")] public string? Alternatives { get; set; }
        [Display(Name = "採用理由")] public string? ReasonForDecision { get; set; }
        [Display(Name = "不採用案と理由")] public string? RejectedAlternatives { get; set; }
        [Display(Name = "参照資料")] public string? References { get; set; }
        [Display(Name = "影響する部署・作業・システム")] public string? Impact { get; set; }
        [Display(Name = "追加対応")] public string? FollowUpActions { get; set; }
        [Display(Name = "担当者")] public string? OwnerName { get; set; }
        [Display(Name = "期限")] public DateOnly? DueDate { get; set; }
        [Display(Name = "見直し条件")] public string? ReviewCondition { get; set; }
        [Display(Name = "作成")] public string? CreatedBy { get; set; }
        [Display(Name = "確認")] public string? ConfirmedBy { get; set; }
        [Display(Name = "決定（承認者）")] public string? ApprovedBy { get; set; }
    }
}
