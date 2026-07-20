using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Cases;

public sealed class BaselinesModel(ApplicationDbContext database, BaselineContentFormatter contentFormatter) : PageModel
{
    public ImprovementCase Case { get; private set; } = null!;
    public IReadOnlyList<GateBaseline> Items { get; private set; } = [];
    public IReadOnlyDictionary<int, IReadOnlyList<BaselineContentItem>> Contents { get; private set; } = new Dictionary<int, IReadOnlyList<BaselineContentItem>>();
    public async Task<IActionResult> OnGetAsync(int caseId)
    {
        var item = await database.Cases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == caseId);
        if (item is null) return NotFound();
        Case = item;
        Items = await database.GateBaselines.AsNoTracking().Where(x => x.CaseId == caseId).OrderByDescending(x => x.ConfirmedAt).ThenByDescending(x => x.Version).ToListAsync();
        Contents = Items.ToDictionary(x => x.Id, x => contentFormatter.Format(x.ContentJson));
        return Page();
    }
}
