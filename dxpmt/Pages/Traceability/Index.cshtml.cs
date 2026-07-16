using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace dxpmt.Pages.Traceability;
public sealed class IndexModel(ApplicationDbContext database):PageModel
{
    [BindProperty(SupportsGet=true)] public int CaseId{get;set;} public ImprovementCase Case{get;private set;}=null!; public List<Row> Rows{get;private set;}=[];
    public async Task<IActionResult> OnGetAsync(){var item=await database.Cases.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==CaseId);if(item is null)return NotFound();Case=item;var links=await database.TraceLinks.AsNoTracking().Where(x=>x.CaseId==CaseId).OrderBy(x=>x.CreatedAt).ToListAsync();var works=await database.WorkItems.AsNoTracking().Where(x=>x.CaseId==CaseId).ToDictionaryAsync(x=>x.Id,x=>x.Name);var problems=await database.Problems.AsNoTracking().Where(x=>x.CaseId==CaseId).ToDictionaryAsync(x=>x.Id,x=>x.Phenomenon);var options=await database.ImprovementOptions.AsNoTracking().Where(x=>x.CaseId==CaseId).ToDictionaryAsync(x=>x.Id,x=>x.Title);var requirements=await database.Requirements.AsNoTracking().Where(x=>x.CaseId==CaseId).ToDictionaryAsync(x=>x.Id,x=>x.Title);Rows=links.Select(x=>new Row(x.SourceType,Name(x.SourceType,x.SourceId),x.TargetType,Name(x.TargetType,x.TargetId),x.CreatedAt)).ToList();return Page();string Name(string type,int id)=>type switch{TraceLinkTypes.WorkItem=>works.GetValueOrDefault(id,"削除済み作業"),TraceLinkTypes.Problem=>problems.GetValueOrDefault(id,"削除済み問題"),TraceLinkTypes.ImprovementOption=>options.GetValueOrDefault(id,"削除済み改善案"),TraceLinkTypes.Requirement=>requirements.GetValueOrDefault(id,"削除済み要求事項"),_=>"不明"};}
    public sealed record Row(string SourceType,string SourceName,string TargetType,string TargetName,DateTime CreatedAt);
}
