using System.DirectoryServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Options;

namespace dxpmt.Services;

public sealed class ActiveDirectoryUserPrincipalNameResolver(IOptions<DxpmtAuthenticationOptions> options, ILogger<ActiveDirectoryUserPrincipalNameResolver> logger)
{
    public async Task<string?> ResolveAsync(string accountName, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(accountName))
        {
            return null;
        }

        var normalizedAccountName = accountName.Trim();
        if (normalizedAccountName.Contains('@'))
        {
            return normalizedAccountName;
        }

        var samAccountName = GhauthUserService.ShortName(normalizedAccountName);
        var configuredUpn = string.IsNullOrWhiteSpace(options.Value.UpnSuffix)
            ? null
            : $"{samAccountName}@{options.Value.UpnSuffix}";

        try
        {
#pragma warning disable CA1416
            return await Task.Run(
                () => FindUserPrincipalName(samAccountName, configuredUpn),
                cancellationToken);
#pragma warning restore CA1416
        }
        catch (Exception exception) when (exception is DirectoryServicesCOMException or System.Runtime.InteropServices.COMException)
        {
            logger.LogWarning(exception, "Active Directory UPN lookup failed for Windows account {AccountName}.", normalizedAccountName);
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private string? FindUserPrincipalName(string samAccountName, string? configuredUpn)
    {
        using var entry = new DirectoryEntry(options.Value.LdapPath ?? "LDAP://RootDSE");
        using var searcher = new DirectorySearcher(entry)
        {
            SearchScope = SearchScope.Subtree,
            Filter = BuildFilter(samAccountName, configuredUpn)
        };
        searcher.PropertiesToLoad.Add("userPrincipalName");
        var result = searcher.FindOne();
        return result is not null
            && result.Properties.Contains("userPrincipalName")
            && result.Properties["userPrincipalName"].Count > 0
            ? result.Properties["userPrincipalName"][0]?.ToString()?.Trim()
            : null;
    }

    private static string BuildFilter(string samAccountName, string? configuredUpn)
    {
        var filters = new List<string> { $"(sAMAccountName={Escape(samAccountName)})" };
        if (!string.IsNullOrWhiteSpace(configuredUpn))
        {
            filters.Add($"(userPrincipalName={Escape(configuredUpn)})");
        }

        return $"(&(objectCategory=person)(objectClass=user)(|{string.Concat(filters)}))";
    }

    private static string Escape(string value) => value
        .Replace(@"\", @"\5c", StringComparison.Ordinal)
        .Replace("*", @"\2a", StringComparison.Ordinal)
        .Replace("(", @"\28", StringComparison.Ordinal)
        .Replace(")", @"\29", StringComparison.Ordinal)
        .Replace("\0", @"\00", StringComparison.Ordinal);
}
