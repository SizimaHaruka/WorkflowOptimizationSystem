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
    public List<GateCount> GatePendingCounts { get; private set; } = [];

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
        GatePendingCounts = Gates.All.Select(gate => new GateCount(
            Gates.GetName(gate),
            cases.Count(x => x.Status is not CaseStatuses.Completed and not CaseStatuses.Cancelled
                && Gates.All.Take(Gates.All.ToList().IndexOf(gate)).All(prior => x.GateReviews.Any(r => r.Gate == prior && r.Decision == GateDecisions.Approved))
                && !x.GateReviews.Any(r => r.Gate == gate && r.Decision == GateDecisions.Approved))))
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        OverdueActionItems = await database.ActionItems
            .Include(x => x.Case)
            .Where(x => x.Status != ActionItemStatuses.Completed && x.DueDate != null && x.DueDate < today)
            .OrderBy(x => x.DueDate)
            .Take(8)
            .ToListAsync();
    }

    public sealed record StatusCount(string Status, int Count);
    public sealed record GateCount(string GateName, int Count);
}
