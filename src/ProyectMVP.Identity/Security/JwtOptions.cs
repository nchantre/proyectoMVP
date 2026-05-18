namespace ProyectMVP.Identity.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ProyectMVP.Api";
    public string Audience { get; set; } = "ProyectMVP.Web";
    public int ExpiryMinutes { get; set; } = 480;
}
