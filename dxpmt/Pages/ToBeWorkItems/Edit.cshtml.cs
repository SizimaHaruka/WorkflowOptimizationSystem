using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.ToBeWorkItems;

public sealed class EditModel(ApplicationDbContext database) : PageModel
{
    [BindProperty] public CreateModel.InputModel Input { get; set; } = new();
    public WorkItem Item { get; private set; } = null!;
    public List<ImprovementOption> ImprovementOptions { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(int id) { if (!await LoadAsync(id)) return NotFound(); var content = Read(Item.ContentJson); Input = new() { ImprovementOptionId = await GetOptionIdAsync(), Name = Item.Name, BusinessProcessName = Item.BusinessProcessName, DepartmentAndRole = Item.DepartmentAndRole, Performer = Item.Performer, Location = Item.Location, IsConfirmed = Item.IsConfirmed, Purpose = content.Purpose, StartTrigger = content.StartTrigger, CompletionCondition = content.CompletionCondition, Handover = content.Handover, Exceptions = content.Exceptions }; return Page(); }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound(); if (Input.ImprovementOptionId.HasValue && !ImprovementOptions.Any(x => x.Id == Input.ImprovementOptionId)) ModelState.AddModelError("Input.ImprovementOptionId", "この案件の改善案を選択してください。"); if (!ModelState.IsValid) return Page();
        Item.Name = Input.Name.Trim(); Item.BusinessProcessName = Input.BusinessProcessName?.Trim() ?? ""; Item.DepartmentAndRole = Input.DepartmentAndRole?.Trim() ?? ""; Item.Performer = Input.Performer?.Trim() ?? ""; Item.Location = Input.Location?.Trim() ?? ""; Item.IsConfirmed = Input.IsConfirmed; Item.ContentJson = JsonSerializer.Serialize(new { Input.Purpose, Input.StartTrigger, Input.CompletionCondition, Input.Handover, Input.Exceptions }); Item.UpdatedAt = DateTime.UtcNow;
        var links = await database.TraceLinks.Where(x => x.CaseId == Item.CaseId && x.TargetType == TraceLinkTypes.WorkItem && x.TargetId == Item.Id && x.SourceType == TraceLinkTypes.ImprovementOption).ToListAsync(); database.TraceLinks.RemoveRange(links); if (Input.ImprovementOptionId is int optionId) database.TraceLinks.Add(new TraceLink { CaseId = Item.CaseId, SourceType = TraceLinkTypes.ImprovementOption, SourceId = optionId, TargetType = TraceLinkTypes.WorkItem, TargetId = Item.Id, CreatedAt = Item.UpdatedAt }); await database.SaveChangesAsync(); return RedirectToPage("Index", new { caseId = Item.CaseId });
    }
    private async Task<bool> LoadAsync(int id) { var item = await database.WorkItems.SingleOrDefaultAsync(x => x.Id == id && x.WorkType == WorkItemTypes.ToBe && !x.IsDeleted); if (item is null) return false; Item = item; ImprovementOptions = await database.ImprovementOptions.Where(x => x.CaseId == item.CaseId).OrderBy(x => x.Sequence).ToListAsync(); return true; }
    private async Task<int?> GetOptionIdAsync() => await database.TraceLinks.Where(x => x.CaseId == Item.CaseId && x.SourceType == TraceLinkTypes.ImprovementOption && x.TargetType == TraceLinkTypes.WorkItem && x.TargetId == Item.Id).Select(x => (int?)x.SourceId).FirstOrDefaultAsync();
    private static ToBeContent Read(string json) { try { return JsonSerializer.Deserialize<ToBeContent>(json) ?? new(); } catch { return new(); } }
    private sealed class ToBeContent { public string? Purpose { get; set; } public string? StartTrigger { get; set; } public string? CompletionCondition { get; set; } public string? Handover { get; set; } public string? Exceptions { get; set; } }
}
