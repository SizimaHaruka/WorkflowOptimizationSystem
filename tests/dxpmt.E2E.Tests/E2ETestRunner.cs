using Microsoft.Playwright;

namespace dxpmt.E2E.Tests;

internal static class E2ETestRunner
{
    public static async Task RunWithPageAsync(E2ETestSettings settings, string testName, Func<IPage, Task> action)
    {
        using var playwright = await Playwright.CreateAsync();
        var host = new Uri(settings.BaseUrl!).Host;
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = settings.Headless,
            Args = [$"--auth-server-whitelist={host}", $"--auth-negotiate-delegate-whitelist={host}"]
        });
        var page = await browser.NewPageAsync();

        try
        {
            await action(page);
        }
        catch
        {
            Directory.CreateDirectory(settings.ArtifactsDirectory);
            var fileName = string.Concat(testName.Select(character => char.IsLetterOrDigit(character) ? character : '_'));
            await File.WriteAllTextAsync(Path.Combine(settings.ArtifactsDirectory, $"{fileName}.html"), await page.ContentAsync());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(settings.ArtifactsDirectory, $"{fileName}.png"), FullPage = true });
            throw;
        }
    }
}
