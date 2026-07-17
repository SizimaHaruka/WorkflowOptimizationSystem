using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.ActionItems;

public sealed class EditModel(ApplicationDbContext database) : PageModel, IActionItemFormPage
{
    [BindProperty]
    public ActionItemInputModel Input { get; set; } = new();
    public ActionItem Item { get; private set; } = null!;
    public IReadOnlyList<string> Categories => ActionItemCategories.All;
    public IReadOnlyList<string> Statuses => ActionItemStatuses.All;
    public int CaseId => Item.CaseId;
    public string SubmitLabel => "更新";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        Input = ActionItemInputModel.From(Item);
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

}
