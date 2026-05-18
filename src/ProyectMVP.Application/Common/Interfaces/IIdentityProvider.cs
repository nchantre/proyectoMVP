using ProyectMVP.Application.Auth;

namespace ProyectMVP.Application.Common.Interfaces;

public interface IIdentityProvider
{
    string ProviderName { get; }

    Task<AuthenticatedUser?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
