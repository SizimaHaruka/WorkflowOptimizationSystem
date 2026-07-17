using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using dxpmt.Domain;

namespace dxpmt.Pages.WorkItems;

public interface IWorkItemFormPage
{
    WorkItemInputModel Input { get; set; }
    int CaseId { get; }
    string SubmitLabel { get; }
}

public sealed class WorkItemInputModel
{
    [Display(Name = "業務・工程名")]
    public string? BusinessProcessName { get; set; }

    [Required(ErrorMessage = "作業名を入力してください。")]
    [Display(Name = "作業名")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "担当部署・役割")]
    public string? DepartmentAndRole { get; set; }

    [Display(Name = "実施者")]
    public string? Performer { get; set; }

    [Display(Name = "実施場所")]
    public string? Location { get; set; }

    [Display(Name = "対象者による内容確認済み")]
    public bool IsConfirmed { get; set; }

    public WorkItemFormModel Content { get; set; } = new();

    public static WorkItemInputModel From(WorkItem item)
    {
        try
        {
            return new WorkItemInputModel
            {
                BusinessProcessName = item.BusinessProcessName,
                Name = item.Name,
                DepartmentAndRole = item.DepartmentAndRole,
                Performer = item.Performer,
                Location = item.Location,
                IsConfirmed = item.IsConfirmed,
                Content = JsonSerializer.Deserialize<WorkItemFormModel>(item.ContentJson) ?? new()
            };
        }
        catch (JsonException)
        {
            return new WorkItemInputModel
            {
                BusinessProcessName = item.BusinessProcessName,
                Name = item.Name,
                DepartmentAndRole = item.DepartmentAndRole,
                Performer = item.Performer,
                Location = item.Location,
                IsConfirmed = item.IsConfirmed
            };
        }
    }
}
