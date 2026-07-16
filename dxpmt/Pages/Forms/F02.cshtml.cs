using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Forms;

public sealed class F02Model(ApplicationDbContext database) : PageModel
{
    [BindProperty]
    public SurveyPlanFormModel Input { get; set; } = new();

    public ImprovementCase Case { get; private set; } = null!;

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int caseId)
    {
        if (!await LoadCaseAsync(caseId))
        {
            return NotFound();
        }

        var form = await database.CaseForms.Where(x => x.CaseId == caseId && x.FormType == FormTypes.SurveyPlan).OrderByDescending(x => x.Version).FirstOrDefaultAsync();
        Input = Deserialize(form?.ContentJson) ?? new SurveyPlanFormModel { SurveyLead = Case.OwnerName };
        Input.EnsureRows();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int caseId)
    {
        if (!await LoadCaseAsync(caseId))
        {
            return NotFound();
        }

        Input.EnsureRows();
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var now = DateTime.UtcNow;
        var form = await database.CaseForms.Where(x => x.CaseId == caseId && x.FormType == FormTypes.SurveyPlan).OrderByDescending(x => x.Version).FirstOrDefaultAsync();
        if (form is null)
        {
            form = new CaseForm { CaseId = caseId, FormType = FormTypes.SurveyPlan, Version = 1 };
            database.CaseForms.Add(form);
        }

        form.ContentJson = JsonSerializer.Serialize(Input);
        form.UpdatedAt = now;
        Case.UpdatedAt = now;
        await database.SaveChangesAsync();

        TempData["SuccessMessage"] = "F02 調査計画票を保存しました。";
        return RedirectToPage(new { caseId });
    }

    private async Task<bool> LoadCaseAsync(int caseId)
    {
        var item = await database.Cases.SingleOrDefaultAsync(x => x.Id == caseId);
        if (item is null)
        {
            return false;
        }

        Case = item;
        return true;
    }

    private static SurveyPlanFormModel? Deserialize(string? contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SurveyPlanFormModel>(contentJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
