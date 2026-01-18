using DaysOff.Data;
using DaysOff.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DaysOff.Services;

public sealed class AuthService
{
    private readonly IDbContextFactory<DaysOffDbContext> _dbFactory;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigation;

    public AuthService(
        IDbContextFactory<DaysOffDbContext> dbFactory,
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigation)
    {
        _dbFactory = dbFactory;
        _authStateProvider = authStateProvider;
        _navigation = navigation;
    }

    public async Task<Guid?> GetCurrentUserIdAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var user = state.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var idValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var id) ? id : null;
    }

    public async Task<(bool Success, string? Error)> RegisterAndSignInAsync(string email, string password)
    {
        email = email.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Email and password are required.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var normalizedEmail = email.ToLowerInvariant();
        var exists = await db.Users.AnyAsync(x => x.Email == normalizedEmail);
        if (exists)
        {
            return (false, "An account with that email already exists.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        await SignInAsync(user);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SignInAsync(string email, string password)
    {
        email = email.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Email and password are required.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var normalizedEmail = email.ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail);
        if (user is null)
        {
            return (false, "Invalid email or password.");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return (false, "Invalid email or password.");
        }

        await SignInAsync(user);
        return (true, null);
    }

    public Task SignOutAsync()
    {
        var relative = _navigation.ToBaseRelativePath(_navigation.Uri);
        var returnUrl = string.IsNullOrWhiteSpace(relative) ? "/" : (relative.StartsWith('/') ? relative : "/" + relative);
        _navigation.NavigateTo($"/logout?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
        return Task.CompletedTask;
    }

    private Task SignInAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var relative = _navigation.ToBaseRelativePath(_navigation.Uri);
        var returnUrl = string.IsNullOrWhiteSpace(relative) ? "/" : (relative.StartsWith('/') ? relative : "/" + relative);
        _navigation.NavigateTo($"/signin?userId={user.Id}&email={Uri.EscapeDataString(user.Email)}&returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
        return Task.CompletedTask;
    }
}
