using System.Security.Claims;

namespace EcomApi.Services;

/// <summary>
/// Centralizes extraction of the current user id from JWT claims so controllers
/// stop repeating the error-prone `int.Parse(User.FindFirstValue(...) !)` pattern.
/// With MapInboundClaims = false the claim type stays the long WS-Identity URI used
/// at token issuance, so ClaimTypes.NameIdentifier resolves consistently.
/// </summary>
public static class CurrentUser
{
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }

    public static string? GetUserEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email);
}
