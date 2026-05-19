using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectMVP.Api.Auth;
using ProyectMVP.Application.Sightings.SearchPlate;
using ProyectMVP.Application.StolenReports.GetByPlate;

namespace ProyectMVP.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/plates")]
public sealed class PlatesController : ControllerBase
{
    private readonly SearchPlateHandler _searchPlateHandler;
    private readonly GetStolenReportsHandler _getStolenReportsHandler;

    public PlatesController(
        SearchPlateHandler searchPlateHandler,
        GetStolenReportsHandler getStolenReportsHandler)
    {
        _searchPlateHandler = searchPlateHandler;
        _getStolenReportsHandler = getStolenReportsHandler;
    }

    [HttpGet("{plate}/sightings")]
    [ProducesResponseType(typeof(SearchPlateResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchPlateResultDto>> GetSightings(string plate, CancellationToken cancellationToken)
    {
        var result = await _searchPlateHandler.HandleAsync(plate, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{plate}/stolen-reports")]
    [ProducesResponseType(typeof(GetStolenReportsResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetStolenReportsResultDto>> GetStolenReports(string plate, CancellationToken cancellationToken)
    {
        var countryIso = User.FindFirst("country_iso")?.Value ?? "CO";
        var result = await _getStolenReportsHandler.HandleAsync(plate, countryIso, cancellationToken);
        return Ok(result);
    }
}
