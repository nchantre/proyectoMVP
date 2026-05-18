using ProyectMVP.Application.Auth;

namespace ProyectMVP.Application.Common.Interfaces;

public interface ITokenIssuer
{
    IssuedToken IssueToken(AuthenticatedUser user);
}

public sealed record IssuedToken(string AccessToken, DateTime ExpiresAtUtc);
