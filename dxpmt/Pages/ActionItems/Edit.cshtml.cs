using System.ComponentModel.DataAnnotations;
using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.ActionItems;

public sealed class EditModel(ApplicationDbContext database) : PageModel
{
    [BindProperty]
    public EditInput Input { get; set; } = new();
    public ActionItem Item { get; private set; } = null!;
    public IReadOnlyList<string> Categories => ActionItemCategories.All;
    public IReadOnlyList<string> Statuses => ActionItemStatuses.All;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        Input = EditInput.From(Item);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        if (!ActionItemCategories.All.Contains(Input.Category)) ModelState.AddModelError("Input.Category", "有効な区分を選択してください。");
        if (!ActionItemStatuses.All.Contains(Input.Status)) ModelState.AddModelError("Input.Status", "有効な状態を選択してください。");
        if (!ModelState.IsValid) return Page();

        Item.RaisedOn = Input.RaisedOn;
        Item.Category = Input.Category;
        Item.Content = Input.Content.Trim();
        Item.ContactOrResponseTarget = Input.ContactOrResponseTarget?.Trim() ?? string.Empty;
        Item.OwnerName = Input.OwnerName?.Trim() ?? string.Empty;
        Item.DueDate = Input.DueDate;
        Item.Status = Input.Status;
        Item.Response = Input.Response?.Trim() ?? string.Empty;
        Item.UpdatedAt = DateTime.UtcNow;
        Item.Case.UpdatedAt = Item.UpdatedAt;
        await database.SaveChangesAsync();
        TempData["SuccessMessage"] = "課題・宿題を更新しました。";
        return RedirectToPage("/ActionItems/Index", new { caseId = Item.CaseId });
    }

    private async Task<bool> LoadAsync(int id)
    {
        var item = await database.ActionItems.Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == id);
        if (item is null) return false;
        Item = item;
        return true;
    }

    public sealed class EditInput
    {
        public DateOnly RaisedOn { get; set; }
        [Required] public string Category { get; set; } = string.Empty;
        [Required(ErrorMessage = "内容を入力してください。"), StringLength(2000)] public string Content { get; set; } = string.Empty;
        [StringLength(200)] public string? ContactOrResponseTarget { get; set; }
        [StringLength(100)] public string? OwnerName { get; set; }
        public DateOnly? DueDate { get; set; }
        [Required] public string Status { get; set; } = string.Empty;
        [StringLength(2000)] public string? Response { get; set; }

        public static EditInput From(ActionItem item) => new()
        {
            RaisedOn = item.RaisedOn, Category = item.Category, Content = item.Content, ContactOrResponseTarget = item.ContactOrResponseTarget,
            OwnerName = item.OwnerName, DueDate = item.DueDate, Status = item.Status, Response = item.Response
        };
    }
}
