using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit;

namespace dxpmt.E2E.Tests;

public sealed class AuthenticationTests
{
    [Fact]
    public async Task Logout_PreventsAutomaticWindowsLogin_AndKeepsLoginPageStyled()
    {
        var settings = E2ETestSettings.Load();
        if (!settings.IsEnabled)
        {
            return;
        }

        await E2ETestRunner.RunWithPageAsync(settings, nameof(Logout_PreventsAutomaticWindowsLogin_AndKeepsLoginPageStyled), async page =>
        {
            await page.GotoAsync($"{settings.BaseUrl}/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "ログアウト" })).ToBeVisibleAsync();

            await page.GetByRole(AriaRole.Button, new() { Name = "ログアウト" }).ClickAsync();
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/Auth/Login\?showLogin=true", RegexOptions.IgnoreCase));
            await Assertions.Expect(page.GetByText("Windows認証で自動ログインできなかったため")).ToBeVisibleAsync();

            var stylesheet = await page.EvaluateAsync<string>("""
                async () => {
                    const response = await fetch('/css/site.css', { cache: 'no-store' });
                    return `${response.status}:${response.headers.get('content-type') ?? ''}`;
                }
                """);
            Assert.StartsWith("200:text/css", stylesheet, StringComparison.OrdinalIgnoreCase);

            await page.GotoAsync($"{settings.BaseUrl}/Cases", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Assertions.Expect(page).ToHaveURLAsync(new Regex(@"/Auth/Login", RegexOptions.IgnoreCase));
            await Assertions.Expect(page.GetByText("Windows認証で自動ログインできなかったため")).ToBeVisibleAsync();
        });
    }
}
