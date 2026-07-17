using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace dxpmt.Pages.Forms;
public sealed class F09Model(ApplicationDbContext database):PageModel
{
    [BindProperty] public EffectConfirmationFormModel Input{get;set;}=new(); public ImprovementCase Case{get;private set;}=null!;
    public async Task<IActionResult> OnGetAsync(int caseId){if(!await Load(caseId))return NotFound();var form=await database.CaseForms.Where(x=>x.CaseId==caseId&&x.FormType==FormTypes.EffectConfirmation).OrderByDescending(x=>x.Version).FirstOrDefaultAsync();Input=Read(form?.ContentJson)??new EffectConfirmationFormModel{Owner=Case.OwnerName};return Page();}
    public async Task<IActionResult> OnPostAsync(int caseId){if(!await Load(caseId))return NotFound();if(!ModelState.IsValid)return Page();var now=DateTime.UtcNow;var form=await database.CaseForms.Where(x=>x.CaseId==caseId&&x.FormType==FormTypes.EffectConfirmation).OrderByDescending(x=>x.Version).FirstOrDefaultAsync();if(form is null){form=new CaseForm{CaseId=caseId,FormType=FormTypes.EffectConfirmation,Version=1};database.CaseForms.Add(form);}form.ContentJson=JsonSerializer.Serialize(Input);form.UpdatedAt=now;Case.UpdatedAt=now;await database.SaveChangesAsync();TempData["SuccessMessage"]="F09 効果確認票を保存しました。";return RedirectToPage(new{caseId});}
    async Task<bool> Load(int id){var item=await database.Cases.SingleOrDefaultAsync(x=>x.Id==id);if(item is null)return false;Case=item;return true;} static EffectConfirmationFormModel? Read(string? json){try{return string.IsNullOrWhiteSpace(json)?null:JsonSerializer.Deserialize<EffectConfirmationFormModel>(json);}catch{return null;}}
}
