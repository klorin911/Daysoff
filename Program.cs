using DaysOff.Components;
using DaysOff.Data;
using DaysOff.Data.Entities;
using DaysOff.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<DaysOffDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DaysOff") ?? "Data Source=daysoff.db"));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<DaysOff.Services.ScheduleService>();
builder.Services.AddScoped<DaysOff.Services.AuthService>();
builder.Services.AddScoped<AccountService>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/login", async (HttpContext http, AccountService accounts) =>
{
    var form = await http.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var (success, error, user) = await accounts.AuthenticateAsync(email, password);
    if (!success || user is null)
    {
        var redirect = QueryHelpers.AddQueryString("/login", new Dictionary<string, string?>
        {
            ["mode"] = "login",
            ["error"] = error ?? "Login failed.",
            ["returnUrl"] = returnUrl
        });

        return Results.Redirect(redirect);
    }

    await SignInUserAsync(http, user);
    return Results.Redirect(GetSafeReturnUrl(returnUrl));
});

app.MapPost("/auth/register", async (HttpContext http, AccountService accounts) =>
{
    var form = await http.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var (success, error, user) = await accounts.RegisterAsync(email, password);
    if (!success || user is null)
    {
        var redirect = QueryHelpers.AddQueryString("/login", new Dictionary<string, string?>
        {
            ["mode"] = "register",
            ["error"] = error ?? "Registration failed.",
            ["returnUrl"] = returnUrl
        });

        return Results.Redirect(redirect);
    }

    await SignInUserAsync(http, user);
    return Results.Redirect(GetSafeReturnUrl(returnUrl));
});

app.MapPost("/auth/logout", async (HttpContext http) =>
{
    var form = await http.Request.ReadFormAsync();
    var returnUrl = form["returnUrl"].ToString();

    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect(GetSafeReturnUrl(returnUrl));
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task SignInUserAsync(HttpContext http, AppUser user)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
}

static string GetSafeReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        return "/schedule";
    }

    // Local-url guard to prevent open redirects.
    if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
    {
        return returnUrl;
    }

    return "/schedule";
}
