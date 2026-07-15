using System.ComponentModel.DataAnnotations;
using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Cases;

public sealed class DetailsModel(ApplicationDbContext database) : PageModel
{
    public ImprovementCase Case { get; private set; } = null!;

    [BindProperty]
    public StatusChangeInput StatusChange { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    public IReadOnlyList<string> StatusOptions => CaseStatuses.All;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadCaseAsync(id) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(int id)
    {
        if (!CaseStatuses.All.Contains(StatusChange.NewStatus))
        {
            ModelState.AddModelError("StatusChange.NewStatus", "有効なステータスを選択してください。");
        }

        if (!ModelState.IsValid)
        {
            return await LoadCaseAsync(id) ? Page() : NotFound();
        }

        var item = await database.Cases.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        if (item.Status == StatusChange.NewStatus)
        {
            ModelState.AddModelError("StatusChange.NewStatus", "現在と異なるステータスを選択してください。");
            return await LoadCaseAsync(id) ? Page() : NotFound();
        }

        var now = DateTime.UtcNow;
        database.CaseStatusHistories.Add(new CaseStatusHistory
        {
            CaseId = id,
            PreviousStatus = item.Status,
            NewStatus = StatusChange.NewStatus,
            ChangedBy = StatusChange.ChangedBy.Trim(),
            Comment = StatusChange.Comment?.Trim() ?? string.Empty,
            ChangedAt = now
        });
        item.Status = StatusChange.NewStatus;
        item.UpdatedAt = now;
        await database.SaveChangesAsync();

        TempData["SuccessMessage"] = "ステータスを更新しました。";
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadCaseAsync(int id)
    {
        var item = await database.Cases
            .Include(x => x.Forms)
            .Include(x => x.ActionItems)
            .Include(x => x.DecisionRecords)
            .Include(x => x.GateReviews)
            .Include(x => x.StatusHistory)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (item is null)
        {
            return false;
        }

        Case = item;
        return true;
    }

    public sealed class StatusChangeInput
    {
        [Required(ErrorMessage = "変更後のステータスを選択してください。")]
        [Display(Name = "変更後ステータス")]
        public string NewStatus { get; set; } = string.Empty;

        [Required(ErrorMessage = "変更者を入力してください。")]
        [StringLength(100)]
        [Display(Name = "変更者")]
        public string ChangedBy { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "コメント")]
        public string? Comment { get; set; }
    }
}
