using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.ImprovementOptions;

public sealed class EditModel(ApplicationDbContext database) : PageModel
{
    [BindProperty] public CreateModel.ImprovementOptionInput Input { get; set; } = new();
    public ImprovementOption Item { get; private set; } = null!;
    public List<Problem> Problems { get; private set; } = [];
    public IReadOnlyList<string> Recommendations => ImprovementRecommendations.All;
    public IReadOnlyList<string> Statuses => ImprovementOptionStatuses.All;
    public async Task<IActionResult> OnGetAsync(int id) { if (!await LoadAsync(id)) return NotFound(); Input = new() { ProblemId = Item.ProblemId, Title = Item.Title, Approach = Item.Approach, ExpectedEffect = Item.ExpectedEffect, CostAndEffort = Item.CostAndEffort, RisksAndConstraints = Item.RisksAndConstraints, Recommendation = Item.Recommendation, Status = Item.Status, Evidence = Item.Evidence }; return Page(); }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        if (Input.ProblemId.HasValue && !Problems.Any(x => x.Id == Input.ProblemId)) ModelState.AddModelError("Input.ProblemId", "この案件の問題を選択してください。");
        if (!Recommendations.Contains(Input.Recommendation) || !Statuses.Contains(Input.Status)) ModelState.AddModelError(string.Empty, "選択値を確認してください。");
        if (!ModelState.IsValid) return Page();
        Item.ProblemId = Input.ProblemId; Item.Title = Input.Title.Trim(); Item.Approach = Input.Approach.Trim(); Item.ExpectedEffect = Input.ExpectedEffect.Trim(); Item.CostAndEffort = Input.CostAndEffort.Trim(); Item.RisksAndConstraints = Input.RisksAndConstraints.Trim(); Item.Recommendation = Input.Recommendation; Item.Status = Input.Status; Item.Evidence = Input.Evidence.Trim(); Item.UpdatedAt = DateTime.UtcNow;
        var links = await database.TraceLinks.Where(x => x.CaseId == Item.CaseId && x.TargetType == TraceLinkTypes.ImprovementOption && x.TargetId == Item.Id).ToListAsync(); database.TraceLinks.RemoveRange(links);
        if (Item.ProblemId is int problemId) database.TraceLinks.Add(new TraceLink { CaseId = Item.CaseId, SourceType = TraceLinkTypes.Problem, SourceId = problemId, TargetType = TraceLinkTypes.ImprovementOption, TargetId = Item.Id, CreatedAt = Item.UpdatedAt });
        await database.SaveChangesAsync(); return RedirectToPage("Index", new { caseId = Item.CaseId });
    }
    private async Task<bool> LoadAsync(int id) { var item = await database.ImprovementOptions.SingleOrDefaultAsync(x => x.Id == id); if (item is null) return false; Item = item; Problems = await database.Problems.Where(x => x.CaseId == item.CaseId && !x.IsDeleted).OrderBy(x => x.Sequence).ToListAsync(); return true; }
}
