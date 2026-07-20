namespace dxpmt.E2E.Tests;

internal sealed record E2ETestSettings(string? BaseUrl, string? ConnectionString, bool Headless, string ArtifactsDirectory)
{
    public bool IsEnabled => !string.IsNullOrWhiteSpace(BaseUrl);
    public bool CanCleanUpDatabase => !string.IsNullOrWhiteSpace(ConnectionString);

    public static E2ETestSettings Load()
    {
        var baseUrl = Environment.GetEnvironmentVariable("DXPMT_E2E_BASE_URL")?.Trim().TrimEnd('/');
        var connectionString = Environment.GetEnvironmentVariable("DXPMT_E2E_CONNECTION_STRING")?.Trim();
        var headless = !string.Equals(Environment.GetEnvironmentVariable("DXPMT_E2E_HEADLESS"), "false", StringComparison.OrdinalIgnoreCase);
        var artifacts = Environment.GetEnvironmentVariable("DXPMT_E2E_ARTIFACTS_DIR");
        return new E2ETestSettings(
            string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl,
            string.IsNullOrWhiteSpace(connectionString) ? null : connectionString,
            headless,
            string.IsNullOrWhiteSpace(artifacts) ? Path.Combine(AppContext.BaseDirectory, "e2e-artifacts") : artifacts);
    }
}
