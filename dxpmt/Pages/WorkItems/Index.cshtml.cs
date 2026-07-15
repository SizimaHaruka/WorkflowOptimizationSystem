using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.WorkItems;

public sealed class IndexModel(ApplicationDbContext database) : PageModel
{
    [BindProperty(SupportsGet = true)] public int CaseId { get; set; }
    [BindProperty(SupportsGet = true)] public bool IncludeDeleted { get; set; }
    public ImprovementCase Case { get; private set; } = null!;
    public List<WorkItem> Items { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync()
    {
        var item = await database.Cases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == CaseId);
        if (item is null) return NotFound();
        Case = item;
        Items = await database.WorkItems.AsNoTracking().Include(x => x.Problems.Where(p => !p.IsDeleted)).Where(x => x.CaseId == CaseId && (IncludeDeleted || !x.IsDeleted)).OrderBy(x => x.Sequence).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var item = await database.WorkItems.SingleOrDefaultAsync(x => x.Id == id && x.CaseId == CaseId && !x.IsDeleted);
        if (item is null) return NotFound();
        item.IsDeleted = true;
        item.DeletedAt = item.UpdatedAt = DateTime.UtcNow;
        await database.SaveChangesAsync();
        TempData["SuccessMessage"] = "F03 の作業を削除しました。";
        return RedirectToPage(new { CaseId, IncludeDeleted });
    }

    public async Task<IActionResult> OnPostRestoreAsync(int id)
    {
        var item = await database.WorkItems.SingleOrDefaultAsync(x => x.Id == id && x.CaseId == CaseId && x.IsDeleted);
        if (item is null) return NotFound();
        item.IsDeleted = false;
        item.DeletedAt = null;
        item.UpdatedAt = DateTime.UtcNow;
        await database.SaveChangesAsync();
        TempData["SuccessMessage"] = "F03 の作業を復旧しました。";
        return RedirectToPage(new { CaseId, IncludeDeleted = true });
    }
}
