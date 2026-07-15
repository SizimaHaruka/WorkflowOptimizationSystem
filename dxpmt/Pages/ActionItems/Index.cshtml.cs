using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.ActionItems;

public sealed class IndexModel(ApplicationDbContext database) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? CaseId { get; set; }

    public ImprovementCase? SelectedCase { get; private set; }
    public List<ActionItem> Items { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (CaseId is not null)
        {
            SelectedCase = await database.Cases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == CaseId);
            if (SelectedCase is null)
            {
                return NotFound();
            }
        }

        var query = database.ActionItems.Include(x => x.Case).AsNoTracking().AsQueryable();
        if (CaseId is not null)
        {
            query = query.Where(x => x.CaseId == CaseId);
        }

        Items = await query
            .OrderBy(x => x.Status == ActionItemStatuses.Completed)
            .ThenBy(x => x.DueDate == null)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.Case.CaseNumber)
            .ThenBy(x => x.Sequence)
            .ToListAsync();

        return Page();
    }
}
