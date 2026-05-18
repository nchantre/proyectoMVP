using ProyectMVP.Application.Common.Interfaces;

namespace ProyectMVP.Application.Auth.Login;

public sealed class LoginHandler
{
    private readonly IIdentityProvider _identityProvider;
    private readonly ITokenIssuer _tokenIssuer;

    public LoginHandler(IIdentityProvider identityProvider, ITokenIssuer tokenIssuer)
    {
        _identityProvider = identityProvider;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<LoginResultDto?> HandleAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = await _identityProvider.AuthenticateAsync(username.Trim(), password, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var issued = _tokenIssuer.IssueToken(user);
        return new LoginResultDto(
            issued.AccessToken,
            issued.ExpiresAtUtc,
            user.Username,
            user.CountryId,
            user.CountryIsoCode,
            _identityProvider.ProviderName,
            user.Roles);
    }
}

public sealed record LoginResultDto(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string Username,
    int CountryId,
    string CountryIsoCode,
    string IdentityProvider,
    IReadOnlyList<string> Roles);
