using System.ComponentModel.DataAnnotations;
using dxpmt.Domain;

namespace dxpmt.Pages.ActionItems;

public interface IActionItemFormPage
{
    ActionItemInputModel Input { get; set; }
    int CaseId { get; }
    IReadOnlyList<string> Categories { get; }
    IReadOnlyList<string> Statuses { get; }
    string SubmitLabel { get; }
}

public sealed class ActionItemInputModel
{
    [Display(Name = "発生日")]
    public DateOnly RaisedOn { get; set; }

    [Required(ErrorMessage = "区分を選択してください。")]
    [Display(Name = "区分")]
    public string Category { get; set; } = ActionItemCategories.Unconfirmed;

    [Required(ErrorMessage = "内容を入力してください。")]
    [StringLength(2000)]
    [Display(Name = "内容")]
    public string Content { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "確認・対応先")]
    public string? ContactOrResponseTarget { get; set; }

    [StringLength(100)]
    [Display(Name = "担当")]
    public string? OwnerName { get; set; }

    [Display(Name = "期限")]
    public DateOnly? DueDate { get; set; }

    [Required(ErrorMessage = "状態を選択してください。")]
    [Display(Name = "状態")]
    public string Status { get; set; } = ActionItemStatuses.NotStarted;

    [StringLength(2000)]
    [Display(Name = "回答・処置")]
    public string? Response { get; set; }

    public static ActionItemInputModel From(ActionItem item) => new()
    {
        RaisedOn = item.RaisedOn,
        Category = item.Category,
        Content = item.Content,
        ContactOrResponseTarget = item.ContactOrResponseTarget,
        OwnerName = item.OwnerName,
        DueDate = item.DueDate,
        Status = item.Status,
        Response = item.Response
    };
}
