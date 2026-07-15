using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages;

public sealed class IndexModel(ApplicationDbContext database) : PageModel
{
    public List<StatusCount> StatusCounts { get; private set; } = [];
    public List<ActionItem> OverdueActionItems { get; private set; } = [];
    public int ActiveCaseCount { get; private set; }
    public int G0PendingCount { get; private set; }
    public int G1PendingCount { get; private set; }

    public async Task OnGetAsync()
    {
        var cases = await database.Cases
            .Include(x => x.GateReviews)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        StatusCounts = cases
            .GroupBy(x => x.Status)
            .OrderBy(x => x.Key)
            .Select(x => new StatusCount(x.Key, x.Count()))
            .ToList();

        ActiveCaseCount = cases.Count(x => x.Status is not CaseStatuses.Completed and not CaseStatuses.OnHold and not CaseStatuses.Cancelled);
        G0PendingCount = cases.Count(x => !x.GateReviews.Any(g => g.Gate == Gates.G0 && g.Decision == GateDecisions.Approved));
        G1PendingCount = cases.Count(x => x.GateReviews.Any(g => g.Gate == Gates.G0 && g.Decision == GateDecisions.Approved)
            && !x.GateReviews.Any(g => g.Gate == Gates.G1 && g.Decision == GateDecisions.Approved));

        var today = DateOnly.FromDateTime(DateTime.Today);
        OverdueActionItems = await database.ActionItems
            .Include(x => x.Case)
            .Where(x => x.Status != ActionItemStatuses.Completed && x.DueDate != null && x.DueDate < today)
            .OrderBy(x => x.DueDate)
            .Take(8)
            .ToListAsync();
    }

    public sealed record StatusCount(string Status, int Count);
}
