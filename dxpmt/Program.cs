using dxpmt.Data;
using dxpmt.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 開発者・運用環境ごとの接続情報は、Git管理しない環境別ローカル設定で上書きする。
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddRazorPages()
    .AddMvcOptions(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<DxpmtAuthenticationOptions>(builder.Configuration.GetSection(DxpmtAuthenticationOptions.SectionName));
builder.Services.Configure<GhauthOptions>(builder.Configuration.GetSection(GhauthOptions.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<GhauthUserService>();
builder.Services.AddScoped<ActiveDirectoryUserPrincipalNameResolver>();
var authentication = builder.Configuration.GetSection(DxpmtAuthenticationOptions.SectionName).Get<DxpmtAuthenticationOptions>() ?? new();
if (authentication.Enabled)
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options => { options.LoginPath = "/Auth/Login"; options.LogoutPath = "/Auth/Logout"; })
        .AddNegotiate();
    builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build());
    builder.Services.AddRazorPages(options => options.Conventions.AllowAnonymousToFolder("/Auth"));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
