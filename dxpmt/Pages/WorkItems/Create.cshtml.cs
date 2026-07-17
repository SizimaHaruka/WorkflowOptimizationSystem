using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.WorkItems;
public sealed class CreateModel(ApplicationDbContext database) : PageModel, IWorkItemFormPage
{
    [BindProperty] public WorkItemInputModel Input { get; set; } = new();
    public ImprovementCase Case { get; private set; } = null!;
    public int CaseId => Case.Id;
    public string SubmitLabel => "保存";
    public async Task<IActionResult> OnGetAsync(int caseId) { if (!await Load(caseId)) return NotFound(); return Page(); }
    public async Task<IActionResult> OnPostAsync(int caseId)
    {
        if (!await Load(caseId)) return NotFound(); if (!ModelState.IsValid) return Page();
        var now=DateTime.UtcNow; var seq=(await database.WorkItems.Where(x=>x.CaseId==caseId).MaxAsync(x=>(int?)x.Sequence)??0)+1;
        database.WorkItems.Add(new WorkItem { CaseId=caseId, Sequence=seq, WorkType=WorkItemTypes.AsIs, BusinessProcessName=Input.BusinessProcessName?.Trim()??"", Name=Input.Name.Trim(), DepartmentAndRole=Input.DepartmentAndRole?.Trim()??"", Performer=Input.Performer?.Trim()??"", Location=Input.Location?.Trim()??"", IsConfirmed=Input.IsConfirmed, ContentJson=JsonSerializer.Serialize(Input.Content), CreatedAt=now, UpdatedAt=now });
        Case.UpdatedAt=now; await database.SaveChangesAsync(); return RedirectToPage("/WorkItems/Index",new{caseId});
    }
    private async Task<bool> Load(int caseId) { var item=await database.Cases.SingleOrDefaultAsync(x=>x.Id==caseId); if(item is null)return false; Case=item; return true; }
}
