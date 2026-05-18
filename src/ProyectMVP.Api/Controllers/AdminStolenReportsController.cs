using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectMVP.Api.Contracts;
using ProyectMVP.Application.StolenReports.Import;

namespace ProyectMVP.Api.Controllers;

[ApiController]
[Authorize(Roles = "AdminImporter")]
[Route("api/v1/admin/stolen-reports")]
public sealed class AdminStolenReportsController : ControllerBase
{
    private readonly ImportStolenReportsHandler _importHandler;

    public AdminStolenReportsController(ImportStolenReportsHandler importHandler)
    {
        _importHandler = importHandler;
    }

    /// <summary>
    /// Importa reportes de hurto desde adaptador JSON o CSV (multi-país).
    /// Demo: usuario admin / contraseña demo.
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportStolenReportsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportStolenReportsResult>> Import(
        [FromBody] ImportStolenReportsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var records = request.Records?
                .Select(r => new StolenVehicleImportRecord(
                    r.Plate.Trim().ToUpperInvariant(),
                    r.TheftDateUtc,
                    r.OwnerDocument.Trim(),
                    string.IsNullOrWhiteSpace(r.CityName) ? null : r.CityName.Trim(),
                    string.IsNullOrWhiteSpace(r.Status) ? "ACTIVE" : r.Status.Trim().ToUpperInvariant(),
                    r.Brand,
                    r.VehicleClass,
                    r.VehicleLine,
                    r.Color,
                    r.ModelYear))
                .ToList();

            var result = await _importHandler.HandleAsync(
                request.Format,
                request.CountryIsoCode,
                request.SourceSystem,
                request.Payload,
                records,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
