using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Requirements;

public sealed class EditModel(ApplicationDbContext database) : PageModel
{
    [BindProperty] public CreateModel.InputModel Input { get; set; } = new();
    public Requirement Item { get; private set; } = null!;
    public List<WorkItem> WorkItems { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(int id) { if (!await LoadAsync(id)) return NotFound(); Input = new() { WorkItemId = Item.WorkItemId, Category = Item.Category, Title = Item.Title, Description = Item.Description, Priority = Item.Priority, AcceptanceCriteria = Item.AcceptanceCriteria, ImplementationApproach = Item.ImplementationApproach, Status = Item.Status }; return Page(); }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound(); if (Input.WorkItemId.HasValue && !WorkItems.Any(x => x.Id == Input.WorkItemId)) ModelState.AddModelError("Input.WorkItemId", "この案件のTo-Be作業を選択してください。"); if (!RequirementCategories.All.Contains(Input.Category) || !RequirementStatuses.All.Contains(Input.Status) || !CasePriorities.All.Contains(Input.Priority)) ModelState.AddModelError(string.Empty, "選択値を確認してください。"); if (!ModelState.IsValid) return Page();
        Item.WorkItemId = Input.WorkItemId; Item.Category = Input.Category; Item.Title = Input.Title.Trim(); Item.Description = Input.Description.Trim(); Item.Priority = Input.Priority; Item.AcceptanceCriteria = Input.AcceptanceCriteria?.Trim() ?? ""; Item.ImplementationApproach = Input.ImplementationApproach?.Trim() ?? ""; Item.Status = Input.Status; Item.UpdatedAt = DateTime.UtcNow;
        var links = await database.TraceLinks.Where(x => x.CaseId == Item.CaseId && x.TargetType == TraceLinkTypes.Requirement && x.TargetId == Item.Id).ToListAsync(); database.TraceLinks.RemoveRange(links); if (Item.WorkItemId is int workItemId) database.TraceLinks.Add(new TraceLink { CaseId = Item.CaseId, SourceType = TraceLinkTypes.WorkItem, SourceId = workItemId, TargetType = TraceLinkTypes.Requirement, TargetId = Item.Id, CreatedAt = Item.UpdatedAt }); await database.SaveChangesAsync(); return RedirectToPage("Index", new { caseId = Item.CaseId });
    }
    private async Task<bool> LoadAsync(int id) { var item = await database.Requirements.SingleOrDefaultAsync(x => x.Id == id); if (item is null) return false; Item = item; WorkItems = await database.WorkItems.Where(x => x.CaseId == item.CaseId && x.WorkType == WorkItemTypes.ToBe && !x.IsDeleted).OrderBy(x => x.Sequence).ToListAsync(); return true; }
}
