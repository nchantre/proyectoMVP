using Microsoft.AspNetCore.Mvc;
using ProyectMVP.Api.Contracts;
using ProyectMVP.Application.Auth.Login;

namespace ProyectMVP.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginHandler _loginHandler;

    public AuthController(LoginHandler loginHandler)
    {
        _loginHandler = loginHandler;
    }

    /// <summary>
    /// Login policía (demo: usuario policia / contraseña demo).
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResultDto>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _loginHandler.HandleAsync(request.Username, request.Password, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        return Ok(result);
    }
}
