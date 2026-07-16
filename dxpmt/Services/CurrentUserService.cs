using Microsoft.AspNetCore.Http;

namespace dxpmt.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
{
    public string DisplayName
    {
        get
        {
            var name = httpContextAccessor.HttpContext?.User.Identity?.Name;
            return string.IsNullOrWhiteSpace(name) ? "システム" : name.Trim();
        }
    }
}
