using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using dxpmt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Cases;

public sealed class CreateModel(ApplicationDbContext database, CurrentUserService currentUser) : PageModel
{
    [BindProperty]
    public CaseInput Input { get; set; } = new();

    public IReadOnlyList<string> Priorities => CasePriorities.All;

    public void OnGet()
    {
        Input.RegisteredOn = DateOnly.FromDateTime(DateTime.Today);
        Input.Priority = CasePriorities.Medium;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var prefix = $"DX-{Input.RegisteredOn.Year:D4}-";
        var sequence = await database.Cases.CountAsync(x => x.CaseNumber.StartsWith(prefix)) + 1;
        var now = DateTime.UtcNow;
        var item = new ImprovementCase
        {
            CaseNumber = $"{prefix}{sequence:D4}",
            Title = Input.Title.Trim(),
            RequestingDepartment = Input.RequestingDepartment.Trim(),
            RequesterName = Input.RequesterName.Trim(),
            OwnerName = Input.OwnerName.Trim(),
            RegisteredOn = Input.RegisteredOn,
            DesiredBy = Input.DesiredBy,
            Priority = Input.Priority,
            Status = CaseStatuses.Received,
            ScopeSummary = Input.ScopeSummary?.Trim() ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };

        database.Cases.Add(item);
        database.CaseForms.Add(new CaseForm
        {
            Case = item,
            FormType = FormTypes.Reception,
            ContentJson = JsonSerializer.Serialize(new ReceptionFormModel { ScopeSummary = item.ScopeSummary }),
            UpdatedAt = now
        });
        database.CaseStatusHistories.Add(new CaseStatusHistory
        {
            Case = item,
            PreviousStatus = string.Empty,
            NewStatus = CaseStatuses.Received,
            ChangedBy = currentUser.DisplayName,
            Comment = "案件を登録しました。",
            ChangedAt = now
        });
        await database.SaveChangesAsync();

        TempData["SuccessMessage"] = $"案件 {item.CaseNumber} を登録しました。続けて F01 案件受付票を記入してください。";
        return RedirectToPage("/Cases/Details", new { id = item.Id });
    }

    public sealed class CaseInput
    {
        [Required(ErrorMessage = "案件名を入力してください。")]
        [StringLength(200)]
        [Display(Name = "案件名")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "依頼部署を入力してください。")]
        [StringLength(100)]
        [Display(Name = "依頼部署")]
        public string RequestingDepartment { get; set; } = string.Empty;

        [Required(ErrorMessage = "依頼者を入力してください。")]
        [StringLength(100)]
        [Display(Name = "依頼者")]
        public string RequesterName { get; set; } = string.Empty;

        [Required(ErrorMessage = "案件責任者を入力してください。")]
        [StringLength(100)]
        [Display(Name = "案件責任者")]
        public string OwnerName { get; set; } = string.Empty;

        [Display(Name = "受付日")]
        public DateOnly RegisteredOn { get; set; }

        [Display(Name = "希望時期")]
        public DateOnly? DesiredBy { get; set; }

        [Required]
        [Display(Name = "緊急度")]
        public string Priority { get; set; } = CasePriorities.Medium;

        [StringLength(2000)]
        [Display(Name = "概略対象範囲")]
        public string? ScopeSummary { get; set; }
    }
}
