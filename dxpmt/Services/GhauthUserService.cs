using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
namespace dxpmt.Services;
public sealed class GhauthUserService(IOptions<GhauthOptions> options, ILogger<GhauthUserService> logger)
{
    public async Task<GhauthUserLookupResult> FindActiveUserAsync(string loginName, CancellationToken ct = default)
    {
        var setting = options.Value;
        if (!setting.Enabled || string.IsNullOrWhiteSpace(setting.ConnectionString))
        {
            logger.LogError("Ghauth lookup was requested, but its connection settings are unavailable.");
            return GhauthUserLookupResult.Unavailable;
        }

        try
        {
            var upn = loginName.Trim();
            var separatorIndex = upn.IndexOf('@');
            if (separatorIndex <= 0 || separatorIndex == upn.Length - 1)
            {
                logger.LogWarning("Ghauth lookup was requested without a valid UPN.");
                return GhauthUserLookupResult.NotFound;
            }

            var upnSuffix = upn[(separatorIndex + 1)..];
            await using var connection = new SqlConnection(setting.ConnectionString);
            await connection.OpenAsync(ct);
            await using var command = new SqlCommand("""
                SELECT TOP (1) employee_name
                FROM dbo.employees
                WHERE LOWER(CONCAT(employee_code, '@', @upnSuffix)) = LOWER(@upn)
                  AND is_active_source = 1
                  AND deleted_source = 0
                  AND is_zaiseki_source = 1;
                """, connection);
            command.Parameters.AddWithValue("@upn", upn);
            command.Parameters.AddWithValue("@upnSuffix", upnSuffix);
            var value = await command.ExecuteScalarAsync(ct);
            return value is string name
                ? new GhauthUserLookupResult(true, name, false)
                : GhauthUserLookupResult.NotFound;
        }
        catch (SqlException exception)
        {
            logger.LogError(exception, "Ghauth lookup failed for UPN {Upn}.", loginName);
            return GhauthUserLookupResult.Unavailable;
        }
    }

    public static string ShortName(string value){var v=value.Trim();var slash=v.LastIndexOf('\\');if(slash>=0)v=v[(slash+1)..];var at=v.IndexOf('@');return at>0?v[..at]:v;}
}

public sealed record GhauthUserLookupResult(bool Found, string DisplayName, bool IsUnavailable)
{
    public static readonly GhauthUserLookupResult NotFound = new(false, string.Empty, false);
    public static readonly GhauthUserLookupResult Unavailable = new(false, string.Empty, true);
}
