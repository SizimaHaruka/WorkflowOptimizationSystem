using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.ToBeWorkItems;
public sealed class CreateModel(ApplicationDbContext database) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public ImprovementCase Case { get; private set; } = null!;
    public List<ImprovementOption> ImprovementOptions { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(int caseId) { if (!await LoadAsync(caseId)) return NotFound(); return Page(); }
    public async Task<IActionResult> OnPostAsync(int caseId)
    {
        if (!await LoadAsync(caseId)) return NotFound();
        if (Input.ImprovementOptionId.HasValue && !ImprovementOptions.Any(x => x.Id == Input.ImprovementOptionId)) ModelState.AddModelError("Input.ImprovementOptionId", "この案件の改善案を選択してください。");
        if (!ModelState.IsValid) return Page();
        var now = DateTime.UtcNow;
        var sequence = (await database.WorkItems.Where(x => x.CaseId == caseId).MaxAsync(x => (int?)x.Sequence) ?? 0) + 1;
        var item = new WorkItem { CaseId = caseId, Sequence = sequence, WorkType = WorkItemTypes.ToBe, BusinessProcessName = Input.BusinessProcessName?.Trim() ?? "", Name = Input.Name.Trim(), DepartmentAndRole = Input.DepartmentAndRole?.Trim() ?? "", Performer = Input.Performer?.Trim() ?? "", Location = Input.Location?.Trim() ?? "", IsConfirmed = Input.IsConfirmed, ContentJson = JsonSerializer.Serialize(new { Input.Purpose, Input.StartTrigger, Input.CompletionCondition, Input.Handover, Input.Exceptions }), CreatedAt = now, UpdatedAt = now };
        database.WorkItems.Add(item); await database.SaveChangesAsync();
        if (Input.ImprovementOptionId.HasValue) { database.TraceLinks.Add(new TraceLink { CaseId = caseId, SourceType = TraceLinkTypes.ImprovementOption, SourceId = Input.ImprovementOptionId.Value, TargetType = TraceLinkTypes.WorkItem, TargetId = item.Id, CreatedAt = now }); await database.SaveChangesAsync(); }
        TempData["SuccessMessage"] = "F07 のTo-Be作業を登録しました。"; return RedirectToPage("Index", new { caseId });
    }
    private async Task<bool> LoadAsync(int caseId) { var item = await database.Cases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == caseId); if (item is null) return false; Case = item; ImprovementOptions = await database.ImprovementOptions.AsNoTracking().Where(x => x.CaseId == caseId).OrderBy(x => x.Sequence).ToListAsync(); return true; }
    public sealed class InputModel { [Display(Name="元となる改善案")] public int? ImprovementOptionId { get; set; } [Required, Display(Name="作業名")] public string Name { get; set; } = ""; [Display(Name="業務・工程名")] public string? BusinessProcessName { get; set; } [Display(Name="担当部署・役割")] public string? DepartmentAndRole { get; set; } [Display(Name="実施者")] public string? Performer { get; set; } [Display(Name="実施場所")] public string? Location { get; set; } [Display(Name="目的")] public string? Purpose { get; set; } [Display(Name="開始条件")] public string? StartTrigger { get; set; } [Display(Name="完了条件")] public string? CompletionCondition { get; set; } [Display(Name="引渡し・出力")] public string? Handover { get; set; } [Display(Name="例外時の扱い")] public string? Exceptions { get; set; } [Display(Name="内容確認済み")] public bool IsConfirmed { get; set; } }
}
