using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Services;

public sealed record GateCompletionCondition(string Label, bool IsSatisfied);

public sealed class GateCompletionService(ApplicationDbContext database)
{
    public async Task<IReadOnlyList<GateCompletionCondition>> GetConditionsAsync(int caseId, string gate)
    {
        return gate switch
        {
            Gates.G3 => await GetG3ConditionsAsync(caseId),
            Gates.G4 => await GetG4ConditionsAsync(caseId),
            Gates.G5 => await GetG5ConditionsAsync(caseId),
            Gates.G6 => await GetG6ConditionsAsync(caseId),
            _ => []
        };
    }

    private async Task<IReadOnlyList<GateCompletionCondition>> GetG3ConditionsAsync(int caseId)
    {
        var problems = await database.Problems.AsNoTracking()
            .Where(x => x.CaseId == caseId && !x.IsDeleted).Select(x => x.Id).ToListAsync();
        var options = await database.ImprovementOptions.AsNoTracking()
            .Where(x => x.CaseId == caseId).Select(x => new { x.Id, x.ProblemId, x.Status }).ToListAsync();
        var linkedProblemIds = await database.TraceLinks.AsNoTracking()
            .Where(x => x.CaseId == caseId && x.SourceType == TraceLinkTypes.Problem && x.TargetType == TraceLinkTypes.ImprovementOption)
            .Select(x => x.SourceId).ToListAsync();

        return
        [
            new("問題が1件以上登録されている", problems.Count > 0),
            new("登録済みの各問題に改善案が紐づいている", problems.Count > 0 && problems.All(id => options.Any(x => x.ProblemId == id) || linkedProblemIds.Contains(id))),
            new("採用する改善案が選定されている", options.Any(x => x.Status == ImprovementOptionStatuses.Selected))
        ];
    }

    private async Task<IReadOnlyList<GateCompletionCondition>> GetG4ConditionsAsync(int caseId)
    {
        var selectedOptionIds = await database.ImprovementOptions.AsNoTracking()
            .Where(x => x.CaseId == caseId && x.Status == ImprovementOptionStatuses.Selected).Select(x => x.Id).ToListAsync();
        var toBeWorkItems = await database.WorkItems.AsNoTracking()
            .Where(x => x.CaseId == caseId && x.WorkType == WorkItemTypes.ToBe && !x.IsDeleted)
            .Select(x => new { x.Id, x.IsConfirmed }).ToListAsync();
        var links = await database.TraceLinks.AsNoTracking()
            .Where(x => x.CaseId == caseId && x.SourceType == TraceLinkTypes.ImprovementOption && x.TargetType == TraceLinkTypes.WorkItem)
            .Select(x => new { x.SourceId, x.TargetId }).ToListAsync();

        return
        [
            new("採用する改善案が選定されている", selectedOptionIds.Count > 0),
            new("採用した各改善案にTo-Be作業が紐づいている", selectedOptionIds.Count > 0 && selectedOptionIds.All(optionId => links.Any(x => x.SourceId == optionId && toBeWorkItems.Any(workItem => workItem.Id == x.TargetId)))),
            new("To-Be作業が1件以上登録され、内容確認済みである", toBeWorkItems.Count > 0 && toBeWorkItems.All(x => x.IsConfirmed))
        ];
    }

    private async Task<IReadOnlyList<GateCompletionCondition>> GetG5ConditionsAsync(int caseId)
    {
        var toBeWorkItemIds = await database.WorkItems.AsNoTracking()
            .Where(x => x.CaseId == caseId && x.WorkType == WorkItemTypes.ToBe && !x.IsDeleted && x.IsConfirmed)
            .Select(x => x.Id).ToListAsync();
        var requirements = await database.Requirements.AsNoTracking()
            .Where(x => x.CaseId == caseId)
            .Select(x => new { x.Id, x.WorkItemId, x.Status, x.AcceptanceCriteria }).ToListAsync();
        var links = await database.TraceLinks.AsNoTracking()
            .Where(x => x.CaseId == caseId && x.SourceType == TraceLinkTypes.WorkItem && x.TargetType == TraceLinkTypes.Requirement)
            .Select(x => new { x.SourceId, x.TargetId }).ToListAsync();

        return
        [
            new("内容確認済みのTo-Be作業が1件以上登録されている", toBeWorkItemIds.Count > 0),
            new("各To-Be作業に要求事項が紐づいている", toBeWorkItemIds.Count > 0 && toBeWorkItemIds.All(workItemId => requirements.Any(x => x.WorkItemId == workItemId) || links.Any(x => x.SourceId == workItemId && requirements.Any(requirement => requirement.Id == x.TargetId)))),
            new("要求事項が1件以上登録され、すべて合意済みである", requirements.Count > 0 && requirements.All(x => x.Status == "合意済み")),
            new("すべての要求事項に受入条件が記録されている", requirements.Count > 0 && requirements.All(x => !string.IsNullOrWhiteSpace(x.AcceptanceCriteria)))
        ];
    }

    private async Task<IReadOnlyList<GateCompletionCondition>> GetG6ConditionsAsync(int caseId)
    {
        var form = await database.CaseForms.AsNoTracking()
            .Where(x => x.CaseId == caseId && x.FormType == FormTypes.EffectConfirmation)
            .OrderByDescending(x => x.Version).FirstOrDefaultAsync();

        return [new("効果確認（F09）が登録されている", form is not null)];
    }
}
