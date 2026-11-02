using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
namespace ILearn.Accounts;

public static class PlayerClaims
{
    public static SessionIdentity Session(this ClaimsPrincipal user) => new(
    Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var player) ? player : Guid.Empty,
    Guid.TryParse(user.FindFirstValue("session"), out var session) ? session : Guid.Empty, user.FindFirstValue("stamp") ?? "");
}
public sealed class CurrentPlayer(AuthenticationStateProvider authentication, IAccounts database) : ICurrentPlayer
{
    public async Task<SessionIdentity> Get()
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User; var session = user.Session();
        if (user.Identity?.IsAuthenticated != true || !await database.ValidSession(session)) throw new UnauthorizedAccessException(); return session;
    }
}