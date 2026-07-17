using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.WorkItems;

public sealed class EditModel(ApplicationDbContext database) : PageModel, IWorkItemFormPage
{
    [BindProperty] public WorkItemInputModel Input { get; set; } = new();
    public WorkItem Item { get; private set; } = null!;
    public int CaseId => Item.CaseId;
    public string SubmitLabel => "更新";
    public async Task<IActionResult> OnGetAsync(int id) { if (!await Load(id)) return NotFound(); Input = WorkItemInputModel.From(Item); return Page(); }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await Load(id)) return NotFound(); if (!ModelState.IsValid) return Page();
        Item.BusinessProcessName = Input.BusinessProcessName?.Trim() ?? ""; Item.Name = Input.Name.Trim(); Item.DepartmentAndRole = Input.DepartmentAndRole?.Trim() ?? ""; Item.Performer = Input.Performer?.Trim() ?? ""; Item.Location = Input.Location?.Trim() ?? ""; Item.IsConfirmed = Input.IsConfirmed; Item.ContentJson = JsonSerializer.Serialize(Input.Content); Item.UpdatedAt = DateTime.UtcNow;
        await database.SaveChangesAsync(); return RedirectToPage("/WorkItems/Index", new { caseId = Item.CaseId });
    }
    private async Task<bool> Load(int id) { var item = await database.WorkItems.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted); if (item is null) return false; Item = item; return true; }
}
