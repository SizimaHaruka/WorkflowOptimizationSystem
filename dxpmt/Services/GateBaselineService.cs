using System.Text.Json;
using dxpmt.Data;
using dxpmt.Domain;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.Services;

public sealed class GateBaselineService(ApplicationDbContext database)
{
    private static readonly IReadOnlyDictionary<string, string[]> FormTypesByGate = new Dictionary<string, string[]>
    {
        [Gates.G0] = [FormTypes.Reception], [Gates.G1] = [FormTypes.SurveyPlan],
        [Gates.G2] = [FormTypes.AsIsWorkItems, FormTypes.AsIsFlow], [Gates.G3] = [FormTypes.Problems, FormTypes.ImprovementOptions],
        [Gates.G4] = [FormTypes.ToBeWorkItems], [Gates.G5] = [FormTypes.ImprovementOptions, FormTypes.Requirements], [Gates.G6] = [FormTypes.EffectConfirmation]
    };

    public IReadOnlyList<string> GetTargets(string gate) => FormTypesByGate.TryGetValue(gate, out var types) ? types : [];

    public async Task CaptureAsync(int caseId, GateReview review, string confirmedBy, DateTime now)
    {
        foreach (var formType in GetTargets(review.Gate))
        {
            var content = await GetContentAsync(caseId, formType, confirmedBy, now);
            if (content is null) continue;
            var version = (await database.GateBaselines.Where(x => x.CaseId == caseId && x.FormType == formType).MaxAsync(x => (int?)x.Version) ?? 0) + 1;
            database.GateBaselines.Add(new GateBaseline { CaseId = caseId, GateReview = review, Gate = review.Gate, FormType = formType, Version = version, ContentJson = content, ConfirmedBy = confirmedBy, ConfirmedAt = now });
        }
    }

    private async Task<string?> GetContentAsync(int caseId, string formType, string confirmedBy, DateTime confirmedAt)
    {
        if (formType is FormTypes.Reception or FormTypes.SurveyPlan or FormTypes.AsIsFlow or FormTypes.EffectConfirmation)
        {
            var form = await database.CaseForms.Where(x => x.CaseId == caseId && x.FormType == formType)
                .OrderByDescending(x => x.Version).FirstOrDefaultAsync();
            if (form is null) return null;

            form.Status = FormStatuses.Confirmed;
            form.ConfirmedBy = confirmedBy;
            form.ConfirmedAt = confirmedAt;
            return form.ContentJson;
        }
        object content = formType switch
        {
            FormTypes.AsIsWorkItems => await database.WorkItems.AsNoTracking().Where(x => x.CaseId == caseId && x.WorkType == WorkItemTypes.AsIs && !x.IsDeleted).OrderBy(x => x.Sequence).ToListAsync(),
            FormTypes.Problems => await database.Problems.AsNoTracking().Where(x => x.CaseId == caseId && !x.IsDeleted).OrderBy(x => x.Sequence).ToListAsync(),
            FormTypes.ImprovementOptions => await database.ImprovementOptions.AsNoTracking().Where(x => x.CaseId == caseId).OrderBy(x => x.Sequence).ToListAsync(),
            FormTypes.ToBeWorkItems => await database.WorkItems.AsNoTracking().Where(x => x.CaseId == caseId && x.WorkType == WorkItemTypes.ToBe && !x.IsDeleted).OrderBy(x => x.Sequence).ToListAsync(),
            FormTypes.Requirements => await database.Requirements.AsNoTracking().Where(x => x.CaseId == caseId).OrderBy(x => x.Sequence).ToListAsync(),
            _ => Array.Empty<object>()
        };
        return JsonSerializer.Serialize(content);
    }
}
