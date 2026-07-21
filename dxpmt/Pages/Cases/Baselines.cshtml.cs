using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Cases;

public sealed class BaselinesModel(ApplicationDbContext database, BaselineContentFormatter contentFormatter, BaselineDiffService diffService) : PageModel
{
    public ImprovementCase Case { get; private set; } = null!;
    public IReadOnlyList<GateBaseline> Items { get; private set; } = [];
    public IReadOnlyDictionary<int, IReadOnlyList<BaselineContentItem>> Contents { get; private set; } = new Dictionary<int, IReadOnlyList<BaselineContentItem>>();
    public IReadOnlyDictionary<int, BaselineComparison> Comparisons { get; private set; } = new Dictionary<int, BaselineComparison>();
    public async Task<IActionResult> OnGetAsync(int caseId)
    {
        var item = await database.Cases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == caseId);
        if (item is null) return NotFound();
        Case = item;
        Items = await database.GateBaselines.AsNoTracking().Where(x => x.CaseId == caseId).OrderByDescending(x => x.ConfirmedAt).ThenByDescending(x => x.Version).ToListAsync();
        Contents = Items.ToDictionary(x => x.Id, x => contentFormatter.Format(x.ContentJson));
        Comparisons = Items
            .GroupBy(x => x.FormType)
            .SelectMany(group => group.OrderBy(x => x.Version).Skip(1).Select(current =>
            {
                var previous = group.Single(x => x.Version == current.Version - 1);
                return new BaselineComparison(current.Id, previous.Version, diffService.Compare(Contents[previous.Id], Contents[current.Id]));
            }))
            .ToDictionary(x => x.BaselineId);
        return Page();
    }
}

public sealed record BaselineComparison(int BaselineId, int PreviousVersion, IReadOnlyList<BaselineDifference> Differences);
