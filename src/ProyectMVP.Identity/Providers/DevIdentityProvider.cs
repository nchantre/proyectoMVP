using ProyectMVP.Application.Auth;
using ProyectMVP.Application.Common.Interfaces;

namespace ProyectMVP.Identity.Providers;

/// <summary>
/// Proveedor de desarrollo. Sustituir por adaptadores LDAP / SOAP / REST por país.
/// </summary>
public sealed class DevIdentityProvider : IIdentityProvider
{
    public string ProviderName => "Dev";

    public Task<AuthenticatedUser?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (password != "demo")
        {
            return Task.FromResult<AuthenticatedUser?>(null);
        }

        if (string.Equals(username, "policia", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<AuthenticatedUser?>(new AuthenticatedUser(
                Username: "policia",
                CountryId: 1,
                CountryIsoCode: "CO",
                Roles: ["PoliceOfficer"]));
        }

        if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<AuthenticatedUser?>(new AuthenticatedUser(
                Username: "admin",
                CountryId: 1,
                CountryIsoCode: "CO",
                Roles: ["AdminImporter", "PoliceOfficer"]));
        }

        return Task.FromResult<AuthenticatedUser?>(null);
    }
}
