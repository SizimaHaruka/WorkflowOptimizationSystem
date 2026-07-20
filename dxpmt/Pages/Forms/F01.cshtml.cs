using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Forms;

public sealed class F01Model(ApplicationDbContext database) : PageModel
{
    [BindProperty]
    public ReceptionFormModel Input { get; set; } = new();

    public ImprovementCase Case { get; private set; } = null!;

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int caseId)
    {
        if (!await LoadCaseAsync(caseId))
        {
            return NotFound();
        }

        var form = await database.CaseForms.Where(x => x.CaseId == caseId && x.FormType == FormTypes.Reception).OrderByDescending(x => x.Version).FirstOrDefaultAsync();
        Input = Deserialize(form?.ContentJson) ?? new ReceptionFormModel { ScopeSummary = Case.ScopeSummary };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int caseId)
    {
        if (!await LoadCaseAsync(caseId))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var now = DateTime.UtcNow;
        var form = await database.CaseForms.Where(x => x.CaseId == caseId && x.FormType == FormTypes.Reception).OrderByDescending(x => x.Version).FirstOrDefaultAsync();
        if (form is null)
        {
            form = new CaseForm { CaseId = caseId, FormType = FormTypes.Reception, Version = 1 };
            database.CaseForms.Add(form);
        }

        form.ContentJson = JsonSerializer.Serialize(Input);
        form.Status = FormStatuses.Draft;
        form.ConfirmedBy = null;
        form.ConfirmedAt = null;
        form.UpdatedAt = now;
        Case.ScopeSummary = Input.ScopeSummary?.Trim() ?? string.Empty;
        Case.UpdatedAt = now;
        await database.SaveChangesAsync();

        TempData["SuccessMessage"] = "F01 案件受付票を保存しました。";
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

    private static ReceptionFormModel? Deserialize(string? contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ReceptionFormModel>(contentJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
