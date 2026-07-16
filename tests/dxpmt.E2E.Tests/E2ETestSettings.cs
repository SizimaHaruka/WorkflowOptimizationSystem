namespace dxpmt.E2E.Tests;

internal sealed record E2ETestSettings(string? BaseUrl, bool Headless, string ArtifactsDirectory)
{
    public bool IsEnabled => !string.IsNullOrWhiteSpace(BaseUrl);

    public static E2ETestSettings Load()
    {
        var baseUrl = Environment.GetEnvironmentVariable("DXPMT_E2E_BASE_URL")?.Trim().TrimEnd('/');
        var headless = !string.Equals(Environment.GetEnvironmentVariable("DXPMT_E2E_HEADLESS"), "false", StringComparison.OrdinalIgnoreCase);
        var artifacts = Environment.GetEnvironmentVariable("DXPMT_E2E_ARTIFACTS_DIR");
        return new E2ETestSettings(
            string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl,
            headless,
            string.IsNullOrWhiteSpace(artifacts) ? Path.Combine(AppContext.BaseDirectory, "e2e-artifacts") : artifacts);
    }
}
