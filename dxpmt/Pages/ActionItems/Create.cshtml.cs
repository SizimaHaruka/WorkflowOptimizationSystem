using System.ComponentModel.DataAnnotations;
using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.ActionItems;

public sealed class CreateModel(ApplicationDbContext database) : PageModel
{
    [BindProperty]
    public ActionItemInput Input { get; set; } = new();

    public ImprovementCase Case { get; private set; } = null!;
    public IReadOnlyList<string> Categories => ActionItemCategories.All;
    public IReadOnlyList<string> Statuses => ActionItemStatuses.All;

    public async Task<IActionResult> OnGetAsync(int caseId)
    {
        if (!await LoadCaseAsync(caseId))
        {
            return NotFound();
        }

        Input.RaisedOn = DateOnly.FromDateTime(DateTime.Today);
        Input.OwnerName = Case.OwnerName;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int caseId)
    {
        if (!await LoadCaseAsync(caseId))
        {
            return NotFound();
        }

        if (!ActionItemCategories.All.Contains(Input.Category))
        {
            ModelState.AddModelError("Input.Category", "有効な区分を選択してください。");
        }
        if (!ActionItemStatuses.All.Contains(Input.Status))
        {
            ModelState.AddModelError("Input.Status", "有効な状態を選択してください。");
        }
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var sequence = (await database.ActionItems.Where(x => x.CaseId == caseId).MaxAsync(x => (int?)x.Sequence) ?? 0) + 1;
        var now = DateTime.UtcNow;
        database.ActionItems.Add(new ActionItem
        {
            CaseId = caseId,
            Sequence = sequence,
            RaisedOn = Input.RaisedOn,
            Category = Input.Category,
            Content = Input.Content.Trim(),
            ContactOrResponseTarget = Input.ContactOrResponseTarget?.Trim() ?? string.Empty,
            OwnerName = Input.OwnerName?.Trim() ?? string.Empty,
            DueDate = Input.DueDate,
            Status = Input.Status,
            Response = Input.Response?.Trim() ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        });
        Case.UpdatedAt = now;
        await database.SaveChangesAsync();
        TempData["SuccessMessage"] = "課題・宿題を追加しました。";
        return RedirectToPage("/ActionItems/Index", new { caseId });
    }

    private async Task<bool> LoadCaseAsync(int caseId)
    {
        var item = await database.Cases.SingleOrDefaultAsync(x => x.Id == caseId);
        if (item is null)
        {
            return false;
        }
        Case = item;
        return true;
    }

    public sealed class ActionItemInput
    {
        [Display(Name = "発生日")]
        public DateOnly RaisedOn { get; set; }
        [Required, Display(Name = "区分")]
        public string Category { get; set; } = ActionItemCategories.Unconfirmed;
        [Required(ErrorMessage = "内容を入力してください。"), StringLength(2000), Display(Name = "内容")]
        public string Content { get; set; } = string.Empty;
        [StringLength(200), Display(Name = "確認・対応先")]
        public string? ContactOrResponseTarget { get; set; }
        [StringLength(100), Display(Name = "担当")]
        public string? OwnerName { get; set; }
        [Display(Name = "期限")]
        public DateOnly? DueDate { get; set; }
        [Required, Display(Name = "状態")]
        public string Status { get; set; } = ActionItemStatuses.NotStarted;
        [StringLength(2000), Display(Name = "回答・処置")]
        public string? Response { get; set; }
    }
}
