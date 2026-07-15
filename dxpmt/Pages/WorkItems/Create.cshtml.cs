using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.WorkItems;
public sealed class CreateModel(ApplicationDbContext database) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public ImprovementCase Case { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(int caseId) { if (!await Load(caseId)) return NotFound(); return Page(); }
    public async Task<IActionResult> OnPostAsync(int caseId)
    {
        if (!await Load(caseId)) return NotFound(); if (!ModelState.IsValid) return Page();
        var now=DateTime.UtcNow; var seq=(await database.WorkItems.Where(x=>x.CaseId==caseId).MaxAsync(x=>(int?)x.Sequence)??0)+1;
        database.WorkItems.Add(new WorkItem { CaseId=caseId, Sequence=seq, BusinessProcessName=Input.BusinessProcessName?.Trim()??"", Name=Input.Name.Trim(), DepartmentAndRole=Input.DepartmentAndRole?.Trim()??"", Performer=Input.Performer?.Trim()??"", Location=Input.Location?.Trim()??"", IsConfirmed=Input.IsConfirmed, ContentJson=JsonSerializer.Serialize(Input.Content), CreatedAt=now, UpdatedAt=now });
        Case.UpdatedAt=now; await database.SaveChangesAsync(); return RedirectToPage("/WorkItems/Index",new{caseId});
    }
    private async Task<bool> Load(int caseId) { var item=await database.Cases.SingleOrDefaultAsync(x=>x.Id==caseId); if(item is null)return false; Case=item; return true; }
    public sealed class InputModel { [Display(Name="業務・工程名")] public string? BusinessProcessName {get;set;} [Required,Display(Name="作業名")] public string Name {get;set;}=""; [Display(Name="担当部署・役割")] public string? DepartmentAndRole {get;set;} [Display(Name="実施者")] public string? Performer {get;set;} [Display(Name="実施場所")] public string? Location {get;set;} [Display(Name="対象者による内容確認済み")] public bool IsConfirmed {get;set;} public WorkItemFormModel Content {get;set;}=new(); }
}
