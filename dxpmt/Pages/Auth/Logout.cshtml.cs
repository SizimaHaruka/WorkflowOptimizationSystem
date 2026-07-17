using Microsoft.AspNetCore.Authentication; using Microsoft.AspNetCore.Authentication.Cookies; using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.Mvc.RazorPages;
using dxpmt.Services;
namespace dxpmt.Pages.Auth;

public sealed class LogoutModel : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Append(AuthenticationCookieNames.ExplicitLogout, "true", new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
        return RedirectToPage("/Auth/Login", new { showLogin = true });
    }
}
