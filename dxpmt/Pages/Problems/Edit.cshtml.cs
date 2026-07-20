using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Problems;

public sealed class EditModel(ApplicationDbContext database) : PageModel
{
    [BindProperty] public CreateModel.InputModel Input { get; set; } = new();
    public Problem Item { get; private set; } = null!;
    public List<WorkItem> WorkItems { get; private set; } = [];
    public IReadOnlyList<string> Categories => ProblemCategories.All;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        Input = new() { WorkItemId = Item.WorkItemId, Category = Item.Category, Phenomenon = Item.Phenomenon, OccurrenceCondition = Item.OccurrenceCondition, Impact = Item.Impact, CurrentWorkaround = Item.CurrentWorkaround, CauseHypothesis = Item.CauseHypothesis, Evidence = Item.Evidence, Severity = Item.Severity, Status = Item.Status };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        if (!ProblemCategories.All.Contains(Input.Category)) ModelState.AddModelError("Input.Category", "分類を選択してください。");
        if (!CasePriorities.All.Contains(Input.Severity)) ModelState.AddModelError("Input.Severity", "重要度を選択してください。");
        if (!ProblemStatuses.All.Contains(Input.Status)) ModelState.AddModelError("Input.Status", "状態を選択してください。");
        if (Input.WorkItemId.HasValue && !WorkItems.Any(x => x.Id == Input.WorkItemId.Value)) ModelState.AddModelError("Input.WorkItemId", "この案件の作業を選択してください。");
        if (!ModelState.IsValid) return Page();

        Item.WorkItemId = Input.WorkItemId; Item.Category = Input.Category; Item.Phenomenon = Input.Phenomenon.Trim(); Item.OccurrenceCondition = Input.OccurrenceCondition?.Trim() ?? ""; Item.Impact = Input.Impact?.Trim() ?? ""; Item.CurrentWorkaround = Input.CurrentWorkaround?.Trim() ?? ""; Item.CauseHypothesis = Input.CauseHypothesis?.Trim() ?? ""; Item.Evidence = Input.Evidence?.Trim() ?? ""; Item.Severity = Input.Severity; Item.Status = Input.Status; Item.UpdatedAt = DateTime.UtcNow;
        var links = await database.TraceLinks.Where(x => x.CaseId == Item.CaseId && x.TargetType == TraceLinkTypes.Problem && x.TargetId == Item.Id).ToListAsync();
        database.TraceLinks.RemoveRange(links);
        if (Item.WorkItemId is int workItemId) database.TraceLinks.Add(new TraceLink { CaseId = Item.CaseId, SourceType = TraceLinkTypes.WorkItem, SourceId = workItemId, TargetType = TraceLinkTypes.Problem, TargetId = Item.Id, CreatedAt = Item.UpdatedAt });
        await database.SaveChangesAsync();
        return RedirectToPage("Index", new { caseId = Item.CaseId });
    }

    private async Task<bool> LoadAsync(int id)
    {
        var item = await database.Problems.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted); if (item is null) return false;
        Item = item;
        WorkItems = await database.WorkItems.Where(x => x.CaseId == item.CaseId && x.WorkType == WorkItemTypes.AsIs && !x.IsDeleted).OrderBy(x => x.Sequence).ToListAsync();
        return true;
    }
}
