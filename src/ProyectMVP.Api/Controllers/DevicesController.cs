using Microsoft.AspNetCore.Mvc;
using ProyectMVP.Api.Contracts;
using ProyectMVP.Application.Sightings.RegisterSighting;

namespace ProyectMVP.Api.Controllers;

[ApiController]
[Route("api/v1/devices")]
public sealed class DevicesController : ControllerBase
{
    private const string ApiKeyHeader = "X-Api-Key";

    private readonly RegisterSightingHandler _registerSightingHandler;

    public DevicesController(RegisterSightingHandler registerSightingHandler)
    {
        _registerSightingHandler = registerSightingHandler;
    }

    /// <summary>
    /// Ingesta de avistamientos desde dispositivo (batch).
    /// Headers: X-Api-Key (demo: DEMO_KEY para dispositivo del seed).
    /// </summary>
    [HttpPost("{deviceId:guid}/sightings/batch")]
    [ProducesResponseType(typeof(RegisterSightingBatchResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RegisterSightingBatchResult>> RegisterSightingsBatch(
        Guid deviceId,
        [FromBody] RegisterSightingBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeader, out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            return Unauthorized(new { message = $"Header {ApiKeyHeader} es obligatorio." });
        }

        var items = request.Items
            .Select(i => new RegisterSightingItem(
                i.Plate,
                i.SeenAtUtc,
                i.Latitude,
                i.Longitude,
                i.Confidence,
                i.EvidenceUrl,
                i.EvidenceType))
            .ToList();

        try
        {
            var result = await _registerSightingHandler.HandleAsync(
                deviceId,
                apiKey.ToString(),
                items,
                cancellationToken);

            return Accepted(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
