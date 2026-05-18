namespace ProyectMVP.Application.Auth;

public sealed record AuthenticatedUser(
    string Username,
    int CountryId,
    string CountryIsoCode,
    IReadOnlyList<string> Roles);
