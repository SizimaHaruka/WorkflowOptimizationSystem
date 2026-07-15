using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.WorkItems;

public sealed class IndexModel(ApplicationDbContext database) : PageModel
{
    [BindProperty(SupportsGet = true)] public int CaseId { get; set; }
    public ImprovementCase Case { get; private set; } = null!;
    public List<WorkItem> Items { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync()
    {
        var item = await database.Cases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == CaseId);
        if (item is null) return NotFound();
        Case = item;
        Items = await database.WorkItems.AsNoTracking().Include(x => x.Problems).Where(x => x.CaseId == CaseId).OrderBy(x => x.Sequence).ToListAsync();
        return Page();
    }
}
