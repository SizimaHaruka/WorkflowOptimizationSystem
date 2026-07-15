using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Decisions;

public sealed class IndexModel(ApplicationDbContext database) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int CaseId { get; set; }

    public ImprovementCase Case { get; private set; } = null!;
    public List<DecisionRecord> Records { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var item = await database.Cases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == CaseId);
        if (item is null) return NotFound();
        Case = item;
        Records = await database.DecisionRecords.AsNoTracking().Where(x => x.CaseId == CaseId).OrderByDescending(x => x.DecidedOn).ThenByDescending(x => x.Sequence).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var record = await database.DecisionRecords.SingleOrDefaultAsync(x => x.Id == id && x.CaseId == CaseId);
        if (record is null) return NotFound();
        database.DecisionRecords.Remove(record);
        await database.SaveChangesAsync();
        return RedirectToPage(new { CaseId });
    }
}
