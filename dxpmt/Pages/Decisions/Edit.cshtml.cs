using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Decisions;

public sealed class EditModel(ApplicationDbContext database, CurrentUserService currentUser) : PageModel
{
    [BindProperty] public CreateModel.DecisionInput Input { get; set; } = new();
    public DecisionRecord Item { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(int id) { if (!await LoadAsync(id)) return NotFound(); Input = From(Item); return Page(); }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound(); Input.CreatedBy = Item.CreatedBy; Input.ApprovedBy = currentUser.DisplayName; ModelState.Remove("Input.CreatedBy"); ModelState.Remove("Input.ApprovedBy"); if (!ModelState.IsValid) return Page();
        Item.DecidedOn=Input.DecidedOn; Item.Title=Input.Title.Trim(); Item.DeciderName=Input.DeciderName.Trim(); Item.Participants=Input.Participants?.Trim()??""; Item.Decision=Input.Decision.Trim(); Item.Scope=Input.Scope?.Trim()??""; Item.EffectiveOn=Input.EffectiveOn; Item.Constraints=Input.Constraints?.Trim()??""; Item.BackgroundAndProblem=Input.BackgroundAndProblem?.Trim()??""; Item.Alternatives=Input.Alternatives?.Trim()??""; Item.ReasonForDecision=Input.ReasonForDecision?.Trim()??""; Item.RejectedAlternatives=Input.RejectedAlternatives?.Trim()??""; Item.References=Input.References?.Trim()??""; Item.Impact=Input.Impact?.Trim()??""; Item.FollowUpActions=Input.FollowUpActions?.Trim()??""; Item.OwnerName=Input.OwnerName?.Trim()??""; Item.DueDate=Input.DueDate; Item.ReviewCondition=Input.ReviewCondition?.Trim()??""; Item.ConfirmedBy=Input.ConfirmedBy?.Trim()??""; Item.ApprovedBy=currentUser.DisplayName; await database.SaveChangesAsync(); return RedirectToPage("Index",new{caseId=Item.CaseId});
    }
    private async Task<bool> LoadAsync(int id) { var item=await database.DecisionRecords.SingleOrDefaultAsync(x=>x.Id==id); if(item is null)return false; Item=item; return true; }
    private static CreateModel.DecisionInput From(DecisionRecord x)=>new(){DecidedOn=x.DecidedOn,Title=x.Title,DeciderName=x.DeciderName,Participants=x.Participants,Decision=x.Decision,Scope=x.Scope,EffectiveOn=x.EffectiveOn,Constraints=x.Constraints,BackgroundAndProblem=x.BackgroundAndProblem,Alternatives=x.Alternatives,ReasonForDecision=x.ReasonForDecision,RejectedAlternatives=x.RejectedAlternatives,References=x.References,Impact=x.Impact,FollowUpActions=x.FollowUpActions,OwnerName=x.OwnerName,DueDate=x.DueDate,ReviewCondition=x.ReviewCondition,CreatedBy=x.CreatedBy,ConfirmedBy=x.ConfirmedBy,ApprovedBy=x.ApprovedBy};
}
