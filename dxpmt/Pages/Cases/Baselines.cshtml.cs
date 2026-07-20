using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Cases;

public sealed class BaselinesModel(ApplicationDbContext database) : PageModel
{
    public ImprovementCase Case { get; private set; } = null!;
    public IReadOnlyList<GateBaseline> Items { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(int caseId)
    {
        var item = await database.Cases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == caseId);
        if (item is null) return NotFound();
        Case = item;
        Items = await database.GateBaselines.AsNoTracking().Where(x => x.CaseId == caseId).OrderByDescending(x => x.ConfirmedAt).ThenByDescending(x => x.Version).ToListAsync();
        return Page();
    }
}
