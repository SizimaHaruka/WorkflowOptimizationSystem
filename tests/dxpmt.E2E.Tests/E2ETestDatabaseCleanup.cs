using dxpmt.Data;
using Microsoft.EntityFrameworkCore;

namespace dxpmt.E2E.Tests;

internal static class E2ETestDatabaseCleanup
{
    public static async Task DeleteCasesByTitleAsync(E2ETestSettings settings, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.ConnectionString);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(settings.ConnectionString)
            .Options;

        await using var database = new ApplicationDbContext(options);
        await using var transaction = await database.Database.BeginTransactionAsync();

        var caseIds = await database.Cases
            .Where(x => x.Title == title)
            .Select(x => x.Id)
            .ToListAsync();

        if (caseIds.Count == 0)
        {
            return;
        }

        // GateBaseline は GateReview への NO ACTION 外部キーを持つため、
        // 案件本体を消す前に関連テーブルを明示的に削除する。
        await database.GateBaselines.Where(x => caseIds.Contains(x.CaseId)).ExecuteDeleteAsync();
        await database.TraceLinks.Where(x => caseIds.Contains(x.CaseId)).ExecuteDeleteAsync();
        await database.Requirements.Where(x => caseIds.Contains(x.CaseId)).ExecuteDeleteAsync();
        await database.ImprovementOptions.Where(x => caseIds.Contains(x.CaseId)).ExecuteDeleteAsync();
        await database.Problems.Where(x => caseIds.Contains(x.CaseId)).ExecuteDeleteAsync();
        await database.WorkItems.Where(x => caseIds.Contains(x.CaseId)).ExecuteDeleteAsync();
        await database.CaseForms.Where(x => caseIds.Contains(x.CaseId)).ExecuteDeleteAsync();
        await database.ActionItems.Where(x => caseIds.Contains(x.CaseId)).ExecuteDeleteAsync();
        await database.DecisionRecords.Where(x => caseIds.Contains(x.CaseId)).ExecuteDeleteAsync();
        await database.CaseStatusHistories.Where(x => caseIds.Contains(x.CaseId)).ExecuteDeleteAsync();
        await database.GateReviews.Where(x => caseIds.Contains(x.CaseId)).ExecuteDeleteAsync();
        await database.Cases.Where(x => caseIds.Contains(x.Id)).ExecuteDeleteAsync();

        await transaction.CommitAsync();
    }
}
