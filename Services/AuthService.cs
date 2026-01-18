using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace DaysOff.Services;

public sealed class AuthService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigation;

    public AuthService(
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigation)
    {
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

    public Task SignOutAsync()
    {
        var relative = _navigation.ToBaseRelativePath(_navigation.Uri);
        var returnUrl = string.IsNullOrWhiteSpace(relative) ? "/" : (relative.StartsWith('/') ? relative : "/" + relative);
        _navigation.NavigateTo($"/logout?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
        return Task.CompletedTask;
    }
}
