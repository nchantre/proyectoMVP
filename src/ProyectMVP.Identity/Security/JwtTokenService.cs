using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProyectMVP.Application.Auth;
using ProyectMVP.Application.Common.Interfaces;

namespace ProyectMVP.Identity.Security;

public sealed class JwtTokenService : ITokenIssuer
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public IssuedToken IssueToken(AuthenticatedUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new("country_id", user.CountryId.ToString()),
            new("country_iso", user.CountryIsoCode)
        };

        foreach (var role in user.Roles)
        {
            // "roles" (plural): ASP.NET Core expande el array JSON del JWT en roles del usuario.
            claims.Add(new Claim("roles", role));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new IssuedToken(accessToken, expiresAt);
    }
}
