using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Cases;

public sealed class IndexModel(ApplicationDbContext database) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public List<ImprovementCase> Cases { get; private set; } = [];
    public Dictionary<int, string> NextGates { get; private set; } = [];
    public IReadOnlyList<string> StatusOptions => CaseStatuses.All;

    public async Task OnGetAsync()
    {
        var query = database.Cases.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(Query))
        {
            query = query.Where(x => x.CaseNumber.Contains(Query) || x.Title.Contains(Query) || x.RequestingDepartment.Contains(Query));
        }

        if (!string.IsNullOrWhiteSpace(Status))
        {
            query = query.Where(x => x.Status == Status);
        }

        Cases = await query.Include(x => x.GateReviews).OrderByDescending(x => x.UpdatedAt).ToListAsync();
        NextGates = Cases.ToDictionary(
            x => x.Id,
            x => Gates.All.FirstOrDefault(g => !x.GateReviews.Where(r => r.Gate == g).OrderByDescending(r => r.CreatedAt).FirstOrDefault()?.Decision.Equals(GateDecisions.Approved, StringComparison.Ordinal) == true) is { } gate
                ? Gates.GetName(gate)
                : "全ゲート完了");
    }
}
