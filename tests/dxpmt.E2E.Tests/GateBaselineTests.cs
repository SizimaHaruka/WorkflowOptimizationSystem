using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit;

namespace dxpmt.E2E.Tests;

public sealed class GateBaselineTests
{
    [Fact]
    public async Task G0Approval_ConfirmsCurrentForm_PreservesBaseline_AndLaterEditReturnsToDraft()
    {
        var settings = E2ETestSettings.Load();
        if (!settings.IsEnabled)
        {
            return;
        }

        if (!settings.CanCleanUpDatabase)
        {
            throw new InvalidOperationException("基準版E2Eテストには DXPMT_E2E_CONNECTION_STRING の指定が必要です。テストで作成した案件を終了時に削除するためです。");
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var title = $"E2E 基準版確認 {suffix}";

        try
        {
            await E2ETestRunner.RunWithPageAsync(settings, nameof(G0Approval_ConfirmsCurrentForm_PreservesBaseline_AndLaterEditReturnsToDraft), async page =>
            {
            var firstProblem = $"初回記録 {suffix}";
            var editedProblem = $"再編集後の記録 {suffix}";

            await page.GotoAsync($"{settings.BaseUrl}/Cases/Create", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GetByLabel("案件名").FillAsync(title);
            await page.GetByLabel("依頼部署").FillAsync("E2Eテスト部署");
            await page.GetByLabel("依頼者").FillAsync("E2Eテスト担当者");
            await page.GetByLabel("案件責任者").FillAsync("E2Eテスト責任者");
            await page.GetByLabel("概略対象範囲").FillAsync("E2Eテスト範囲");
            await page.GetByRole(AriaRole.Button, new() { Name = "案件を登録して F01 へ" }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/Cases/Details/\d+", RegexOptions.IgnoreCase));

            var caseId = new Uri(page.Url).Segments.Last().Trim('/');
            await page.GotoAsync($"{settings.BaseUrl}/Forms/F01?caseId={caseId}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GetByLabel("困っていること／改善したいこと").FillAsync(firstProblem);
            await page.GetByLabel("発生する場面").FillAsync("月次処理");
            await page.GetByLabel("対象業務・部署").FillAsync("E2Eテスト部署");
            await page.GetByLabel("現在の対応方法").FillAsync("手作業");
            await page.GetByLabel("期待する状態").FillAsync("自動化");
            await page.GetByLabel("概略対象範囲").FillAsync("E2Eテスト範囲");
            await page.GetByRole(AriaRole.Button, new() { Name = "F01を保存" }).ClickAsync();

            await page.GotoAsync($"{settings.BaseUrl}/Cases/Gate?caseId={caseId}&gate=G0", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GetByLabel("問題が明確である").CheckAsync();
            await page.GetByLabel("依頼者が明確である").CheckAsync();
            await page.GetByLabel("案件責任者が明確である").CheckAsync();
            await page.GetByLabel("概略範囲が明確である").CheckAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "判定を記録" }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex($@"/Cases/Details/{caseId}$", RegexOptions.IgnoreCase));
            await Assertions.Expect(page.GetByText("確認済み", new() { Exact = true })).ToBeVisibleAsync();

            await page.GetByRole(AriaRole.Link, new() { Name = "基準版履歴" }).ClickAsync();
            await page.GetByText("解決したい問題", new() { Exact = true }).WaitForAsync();
            await Assertions.Expect(page.GetByText(firstProblem, new() { Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText("ProblemToSolve", new() { Exact = true })).Not.ToBeVisibleAsync();

            await page.GotoAsync($"{settings.BaseUrl}/Forms/F01?caseId={caseId}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GetByLabel("困っていること／改善したいこと").FillAsync(editedProblem);
            await page.GetByRole(AriaRole.Button, new() { Name = "F01を保存" }).ClickAsync();
            await page.GotoAsync($"{settings.BaseUrl}/Cases/Details/{caseId}", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await Assertions.Expect(page.GetByText("下書き", new() { Exact = true })).ToBeVisibleAsync();

            await page.GetByRole(AriaRole.Link, new() { Name = "基準版履歴" }).ClickAsync();
            await Assertions.Expect(page.GetByText(firstProblem, new() { Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(editedProblem, new() { Exact = true })).Not.ToBeVisibleAsync();
            });
        }
        finally
        {
            await E2ETestDatabaseCleanup.DeleteCasesByTitleAsync(settings, title);
        }
    }
}
