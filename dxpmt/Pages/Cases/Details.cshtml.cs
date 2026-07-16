using System.ComponentModel.DataAnnotations;
using System.Text;
using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Pages.Cases;

public sealed class DetailsModel(ApplicationDbContext database) : PageModel
{
    public ImprovementCase Case { get; private set; } = null!;
    public int ImprovementOptionCount { get; private set; }
    public int ToBeWorkItemCount { get; private set; }
    public int RequirementCount { get; private set; }

    [BindProperty]
    public StatusChangeInput StatusChange { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    public IReadOnlyList<string> StatusOptions => CaseStatuses.All;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadCaseAsync(id) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnGetExportMarkdownAsync(int id)
    {
        if (!await LoadCaseAsync(id)) return NotFound();

        var markdown = new StringBuilder();
        markdown.AppendLine($"# {Case.CaseNumber} {Case.Title}");
        markdown.AppendLine();
        markdown.AppendLine("## 案件情報");
        markdown.AppendLine($"- ステータス: {Case.Status}");
        markdown.AppendLine($"- 依頼部署: {Case.RequestingDepartment}");
        markdown.AppendLine($"- 依頼者: {Case.RequesterName}");
        markdown.AppendLine($"- 責任者: {Case.OwnerName}");
        markdown.AppendLine($"- 概要範囲: {Case.ScopeSummary}");

        markdown.AppendLine();
        markdown.AppendLine("## ゲート判定履歴");
        foreach (var review in Case.GateReviews.OrderBy(x => x.CreatedAt))
            markdown.AppendLine($"- {Gates.GetName(review.Gate)}: {review.Decision}（{review.ReviewedOn:yyyy/MM/dd} / {review.ReviewerName}） {review.Comment}");

        markdown.AppendLine();
        markdown.AppendLine("## 帳票データ");
        foreach (var form in Case.Forms.OrderBy(x => x.FormType).ThenBy(x => x.Version))
        {
            markdown.AppendLine($"### {form.FormType}（第{form.Version}版 / {form.Status}）");
            markdown.AppendLine("```json");
            markdown.AppendLine(form.ContentJson);
            markdown.AppendLine("```");
        }

        markdown.AppendLine("## 課題・宿題");
        foreach (var action in Case.ActionItems.OrderBy(x => x.Sequence)) markdown.AppendLine($"- [{action.Status}] {action.Content}（担当: {action.OwnerName}）");
        markdown.AppendLine("## 意思決定記録");
        foreach (var decision in Case.DecisionRecords.OrderBy(x => x.Sequence)) markdown.AppendLine($"- {decision.DecidedOn:yyyy/MM/dd} {decision.Title}: {decision.Decision}");

        return File(Encoding.UTF8.GetBytes(markdown.ToString()), "text/markdown; charset=utf-8", $"{Case.CaseNumber}_案件記録.md");
    }

    public async Task<IActionResult> OnGetExportCsvAsync(int id)
    {
        var item = await database.Cases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        var rows = new List<string[]> { new[] { "種別", "番号", "名称・現象", "内容", "状態", "優先度・推奨度" } };
        var workItems = await database.WorkItems.AsNoTracking().Where(x => x.CaseId == id && !x.IsDeleted).OrderBy(x => x.Sequence).ToListAsync();
        rows.AddRange(workItems.Select(x => new[] { x.WorkType == WorkItemTypes.ToBe ? "To-Be作業" : "As-Is作業", x.Sequence.ToString(), x.Name, x.DepartmentAndRole, x.IsConfirmed ? "確認済み" : "未確認", string.Empty }));
        var problems = await database.Problems.AsNoTracking().Where(x => x.CaseId == id && !x.IsDeleted).OrderBy(x => x.Sequence).ToListAsync();
        rows.AddRange(problems.Select(x => new[] { "問題", x.Sequence.ToString(), x.Phenomenon, x.Impact, x.Status, x.Severity }));
        var options = await database.ImprovementOptions.AsNoTracking().Where(x => x.CaseId == id).OrderBy(x => x.Sequence).ToListAsync();
        rows.AddRange(options.Select(x => new[] { "改善案", x.Sequence.ToString(), x.Title, x.ExpectedEffect, x.Status, x.Recommendation }));
        var requirements = await database.Requirements.AsNoTracking().Where(x => x.CaseId == id).OrderBy(x => x.Sequence).ToListAsync();
        rows.AddRange(requirements.Select(x => new[] { "要求事項", x.Sequence.ToString(), x.Title, x.Description, x.Status, x.Priority }));
        var csv = string.Join(Environment.NewLine, rows.Select(row => string.Join(',', row.Select(EscapeCsv))));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv; charset=utf-8", $"{item.CaseNumber}_案件一覧.csv");
    }

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

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
        ImprovementOptionCount = await database.ImprovementOptions.CountAsync(x => x.CaseId == id);
        ToBeWorkItemCount = await database.WorkItems.CountAsync(x => x.CaseId == id && x.WorkType == WorkItemTypes.ToBe && !x.IsDeleted);
        RequirementCount = await database.Requirements.CountAsync(x => x.CaseId == id);
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
