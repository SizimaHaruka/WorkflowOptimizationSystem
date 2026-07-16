using System.ComponentModel.DataAnnotations;
using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.ImprovementOptions;

public sealed class CreateModel(ApplicationDbContext database) : PageModel
{
    [BindProperty]
    public ImprovementOptionInput Input { get; set; } = new();

    public ImprovementCase Case { get; private set; } = null!;
    public List<Problem> Problems { get; private set; } = [];
    public IReadOnlyList<string> Recommendations => ImprovementRecommendations.All;
    public IReadOnlyList<string> Statuses => ImprovementOptionStatuses.All;

    public async Task<IActionResult> OnGetAsync(int caseId)
    {
        if (!await LoadAsync(caseId)) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int caseId)
    {
        if (!await LoadAsync(caseId)) return NotFound();
        if (Input.ProblemId.HasValue && !Problems.Any(x => x.Id == Input.ProblemId.Value))
            ModelState.AddModelError("Input.ProblemId", "この案件の問題を選択してください。");
        if (!Recommendations.Contains(Input.Recommendation))
            ModelState.AddModelError("Input.Recommendation", "推奨度を選択してください。");
        if (!Statuses.Contains(Input.Status))
            ModelState.AddModelError("Input.Status", "状態を選択してください。");
        if (!ModelState.IsValid) return Page();

        var now = DateTime.UtcNow;
        var sequence = (await database.ImprovementOptions.Where(x => x.CaseId == caseId).MaxAsync(x => (int?)x.Sequence) ?? 0) + 1;
        var item = new ImprovementOption
        {
            CaseId = caseId,
            ProblemId = Input.ProblemId,
            Sequence = sequence,
            Title = Input.Title.Trim(),
            Approach = Input.Approach.Trim(),
            ExpectedEffect = Input.ExpectedEffect.Trim(),
            CostAndEffort = Input.CostAndEffort.Trim(),
            RisksAndConstraints = Input.RisksAndConstraints.Trim(),
            Recommendation = Input.Recommendation,
            Status = Input.Status,
            Evidence = Input.Evidence.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        database.ImprovementOptions.Add(item);
        await database.SaveChangesAsync();

        if (item.ProblemId.HasValue)
        {
            database.TraceLinks.Add(new TraceLink
            {
                CaseId = caseId,
                SourceType = TraceLinkTypes.Problem,
                SourceId = item.ProblemId.Value,
                TargetType = TraceLinkTypes.ImprovementOption,
                TargetId = item.Id,
                CreatedAt = now
            });
            await database.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "F06 の改善案を登録しました。";
        return RedirectToPage("Index", new { caseId });
    }

    private async Task<bool> LoadAsync(int caseId)
    {
        var item = await database.Cases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == caseId);
        if (item is null) return false;
        Case = item;
        Problems = await database.Problems.AsNoTracking().Where(x => x.CaseId == caseId && !x.IsDeleted).OrderBy(x => x.Sequence).ToListAsync();
        return true;
    }

    public sealed class ImprovementOptionInput
    {
        [Display(Name = "対象問題")]
        public int? ProblemId { get; set; }
        [Required(ErrorMessage = "改善案の名称を入力してください。"), StringLength(200), Display(Name = "改善案の名称")]
        public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "改善の方針を入力してください。"), Display(Name = "改善の方針")]
        public string Approach { get; set; } = string.Empty;
        [Required(ErrorMessage = "期待効果を入力してください。"), Display(Name = "期待効果")]
        public string ExpectedEffect { get; set; } = string.Empty;
        [Display(Name = "費用・工数")]
        public string CostAndEffort { get; set; } = string.Empty;
        [Display(Name = "リスク・制約")]
        public string RisksAndConstraints { get; set; } = string.Empty;
        [Required, Display(Name = "推奨度")]
        public string Recommendation { get; set; } = ImprovementRecommendations.ToBeEvaluated;
        [Required, Display(Name = "検討状態")]
        public string Status { get; set; } = ImprovementOptionStatuses.Draft;
        [Display(Name = "根拠・参考情報")]
        public string Evidence { get; set; } = string.Empty;
    }
}
