using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectMVP.Api.Contracts;
using ProyectMVP.Application.StolenReports.Import;
using Swashbuckle.AspNetCore.Annotations;

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
    /// Importa reportes de hurto (lote). Requiere login <b>admin</b> / demo.
    /// </summary>
    /// <remarks>
    /// **Formatos:** `json` (arreglo `records`) o `csv` (texto en `payload`).
    ///
    /// **Respuesta:** revise `summary` y el detalle en `items[].outcome`:
    /// - `created` — reporte nuevo insertado
    /// - `updated_previous_active` — se cerró un hurto ACTIVE previo y se creó el nuevo
    /// - `skipped_duplicate` — ya existía (misma placa, fecha y documento)
    /// - `error` — validación o fallo en esa fila
    /// </remarks>
    [HttpPost("import")]
    [SwaggerOperation(Summary = "Importar hurtos por lote", OperationId = "ImportStolenReports")]
    [SwaggerResponse(StatusCodes.Status200OK, "Resultado por fila en items[]", typeof(ImportStolenReportsResult))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Request inválido")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Sin token JWT")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Token sin rol AdminImporter (use usuario admin)")]
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
