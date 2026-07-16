using System.ComponentModel.DataAnnotations;
using System.DirectoryServices;
using System.Runtime.Versioning;
using System.Security.Claims;
using dxpmt.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace dxpmt.Pages.Auth;

public sealed class LoginModel(
    IOptions<DxpmtAuthenticationOptions> options,
    GhauthUserService users,
    ActiveDirectoryUserPrincipalNameResolver upnResolver) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public bool WindowsEnabled => options.Value.WindowsAuthenticationEnabled;

    public IActionResult OnGet(string? returnUrl = null) => User.Identity?.IsAuthenticated == true ? LocalRedirect(returnUrl ?? "/") : Page();

    public IActionResult OnPostWindows(string? returnUrl = null) => WindowsEnabled
        ? RedirectToPage("Login", "WindowsCallback", new { returnUrl, attempted = false })
        : Page();

    public async Task<IActionResult> OnGetWindowsCallbackAsync(string? returnUrl = null, bool attempted = false)
    {
        var result = await HttpContext.AuthenticateAsync(NegotiateDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal?.Identity?.IsAuthenticated != true)
        {
            if (!attempted)
            {
                var callback = Url.Page("/Auth/Login", "WindowsCallback", new { returnUrl, attempted = true }) ?? "/Auth/Login";
                return Challenge(new AuthenticationProperties { RedirectUri = callback }, NegotiateDefaults.AuthenticationScheme);
            }

            ModelState.AddModelError(string.Empty, "Windows認証に失敗しました。ADアカウントでログインしてください。");
            return Page();
        }

        var accountName = result.Principal.FindFirstValue(ClaimTypes.WindowsAccountName) ?? result.Principal.Identity.Name;
        var directUpn = result.Principal.FindFirstValue(ClaimTypes.Upn)
            ?? result.Principal.FindFirstValue("upn")
            ?? result.Principal.FindFirstValue(ClaimTypes.Email);
        var upn = !string.IsNullOrWhiteSpace(directUpn)
            ? directUpn.Trim()
            : !string.IsNullOrWhiteSpace(accountName)
                ? await upnResolver.ResolveAsync(accountName) ?? ToUpn(accountName)
                : null;
        if (string.IsNullOrWhiteSpace(upn))
        {
            ModelState.AddModelError(string.Empty, "Windows認証から利用者情報を取得できませんでした。ADアカウントでログインしてください。");
            return Page();
        }

        return await SignInAsync(upn, returnUrl);
    }

    public async Task<IActionResult> OnPostLocalAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();
        if (!OperatingSystem.IsWindows() || !ValidateWindowsCredentials(Input.UserName, Input.Password))
        {
            ModelState.AddModelError(string.Empty, "ユーザー名またはパスワードが正しくありません。");
            return Page();
        }

        return await SignInAsync(ToUpn(Input.UserName), returnUrl);
    }

    private async Task<IActionResult> SignInAsync(string upn, string? returnUrl)
    {
        var user = await users.FindActiveUserAsync(upn);
        if (user.IsUnavailable)
        {
            ModelState.AddModelError(string.Empty, "ユーザー管理基盤に接続できません。管理者へ連絡してください。");
            return Page();
        }

        if (!user.Found)
        {
            ModelState.AddModelError(string.Empty, "Ghauthに有効な利用者として登録されていません。");
            return Page();
        }

        var claims = new[] { new Claim(ClaimTypes.Name, user.DisplayName), new Claim(ClaimTypes.Upn, upn) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }

    private string ToUpn(string value) => value.Contains('@') ? value.Trim() : $"{GhauthUserService.ShortName(value)}@{options.Value.UpnSuffix}";

    private bool ValidateWindowsCredentials(string userName, string password)
    {
        foreach (var candidate in new[] { ToUpn(userName), $"{options.Value.DomainName}\\{GhauthUserService.ShortName(userName)}" }.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (TryBind(candidate, password)) return true;
        }
        return false;
    }

    private bool TryBind(string credentialName, string password)
    {
#pragma warning disable CA1416
        try { using var entry = new DirectoryEntry(options.Value.LdapPath ?? "LDAP://RootDSE", credentialName, password, AuthenticationTypes.Secure); _ = entry.NativeObject; return true; }
        catch { return false; }
#pragma warning restore CA1416
    }

    public sealed class InputModel
    {
        [Required, Display(Name = "ADユーザー名")] public string UserName { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), Display(Name = "パスワード")] public string Password { get; set; } = string.Empty;
    }
}
