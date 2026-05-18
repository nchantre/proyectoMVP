namespace ProyectMVP.Api.Contracts;

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
