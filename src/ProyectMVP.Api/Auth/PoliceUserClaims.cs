using System.Security.Claims;

namespace ProyectMVP.Api.Auth;

public static class PoliceUserClaims
{
    public const string CountryId = "country_id";
    public const string CountryIso = "country_iso";

    public static int GetCountryId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(CountryId);
        if (!int.TryParse(value, out var countryId) || countryId <= 0)
        {
            throw new UnauthorizedAccessException("Token sin país asignado.");
        }

        return countryId;
    }
}
