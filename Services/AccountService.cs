using DaysOff.Data;
using DaysOff.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DaysOff.Services;

public sealed class AccountService
{
    private readonly IDbContextFactory<DaysOffDbContext> _dbFactory;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public AccountService(IDbContextFactory<DaysOffDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<(bool Success, string? Error, AppUser? User)> AuthenticateAsync(string email, string password)
    {
        email = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Email and password are required.", null);
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
        if (user is null)
        {
            return (false, "Invalid email or password.", null);
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return (false, "Invalid email or password.", null);
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await db.SaveChangesAsync();
        }

        return (true, null, user);
    }

    public async Task<(bool Success, string? Error, AppUser? User)> RegisterAsync(string email, string password)
    {
        email = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Email and password are required.", null);
        }

        if (password.Length < 6)
        {
            return (false, "Password must be at least 6 characters.", null);
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var exists = await db.Users.AnyAsync(x => x.Email == email);
        if (exists)
        {
            return (false, "An account with that email already exists.", null);
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (true, null, user);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
