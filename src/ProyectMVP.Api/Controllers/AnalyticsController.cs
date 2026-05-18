using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectMVP.Api.Auth;
using ProyectMVP.Application.Analytics.Hotspots;
using Swashbuckle.AspNetCore.Annotations;

namespace ProyectMVP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly GetHotspotsHandler _getHotspotsHandler;

    public AnalyticsController(GetHotspotsHandler getHotspotsHandler)
    {
        _getHotspotsHandler = getHotspotsHandler;
    }

    /// <summary>
    /// Hotspots para mapas de calor y tendencias (hurtos, avistamientos, posibles matches).
    /// </summary>
    /// <remarks>
    /// Requiere JWT de policía. Solo datos del país del usuario.
    ///
    /// **category** (opcional): `all` (default), `theft`, `sighting`, `potential_match`
    ///
    /// **from** / **to** (opcional, UTC): rango de fechas. Por defecto últimos 30 días.
    ///
    /// **intensity** (0–1): relativo al máximo count del resultado (útil para capas de calor).
    /// </remarks>
    [HttpGet("hotspots")]
    [SwaggerOperation(Summary = "Hotspots analíticos", OperationId = "GetAnalyticsHotspots")]
    [SwaggerResponse(StatusCodes.Status200OK, "Lista de puntos agregados", typeof(GetHotspotsResultDto))]
    [ProducesResponseType(typeof(GetHotspotsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetHotspotsResultDto>> GetHotspots(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        try
        {
            var countryId = PoliceUserClaims.GetCountryId(User);
            var countryIso = User.FindFirst("country_iso")?.Value ?? "CO";

            var result = await _getHotspotsHandler.HandleAsync(
                countryId,
                countryIso,
                from,
                to,
                category,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
