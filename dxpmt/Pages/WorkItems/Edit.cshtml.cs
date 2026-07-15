using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.WorkItems;

public sealed class EditModel(ApplicationDbContext database) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public WorkItem Item { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(int id) { if (!await Load(id)) return NotFound(); Input = InputModel.From(Item); return Page(); }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await Load(id)) return NotFound(); if (!ModelState.IsValid) return Page();
        Item.BusinessProcessName = Input.BusinessProcessName?.Trim() ?? ""; Item.Name = Input.Name.Trim(); Item.DepartmentAndRole = Input.DepartmentAndRole?.Trim() ?? ""; Item.Performer = Input.Performer?.Trim() ?? ""; Item.Location = Input.Location?.Trim() ?? ""; Item.IsConfirmed = Input.IsConfirmed; Item.ContentJson = JsonSerializer.Serialize(Input.Content); Item.UpdatedAt = DateTime.UtcNow;
        await database.SaveChangesAsync(); return RedirectToPage("/WorkItems/Index", new { caseId = Item.CaseId });
    }
    private async Task<bool> Load(int id) { var item = await database.WorkItems.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted); if (item is null) return false; Item = item; return true; }
    public sealed class InputModel { [Display(Name="業務・工程名")] public string? BusinessProcessName { get; set; } [Required(ErrorMessage="作業名を入力してください。"), Display(Name="作業名")] public string Name { get; set; } = ""; [Display(Name="担当部署・役割")] public string? DepartmentAndRole { get; set; } [Display(Name="実施者")] public string? Performer { get; set; } [Display(Name="実施場所")] public string? Location { get; set; } [Display(Name="対象者による内容確認済み")] public bool IsConfirmed { get; set; } public WorkItemFormModel Content { get; set; } = new(); public static InputModel From(WorkItem item) { WorkItemFormModel content; try { content = JsonSerializer.Deserialize<WorkItemFormModel>(item.ContentJson) ?? new(); } catch { content = new(); } return new() { BusinessProcessName=item.BusinessProcessName, Name=item.Name, DepartmentAndRole=item.DepartmentAndRole, Performer=item.Performer, Location=item.Location, IsConfirmed=item.IsConfirmed, Content=content }; } }
}
