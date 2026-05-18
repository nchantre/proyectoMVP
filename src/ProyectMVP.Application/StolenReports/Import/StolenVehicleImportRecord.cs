namespace ProyectMVP.Application.StolenReports.Import;

public sealed record StolenVehicleImportRecord(
    string Plate,
    DateTime TheftDateUtc,
    string OwnerDocument,
    string? CityName,
    string Status,
    string? Brand,
    string? VehicleClass,
    string? VehicleLine,
    string? Color,
    int? ModelYear);

/// <summary>Resultado por cada fila importada (visible en Swagger).</summary>
public sealed record ImportStolenReportItemResult(
    string Plate,
    string Outcome,
    string Message,
    long? StolenVehicleReportId = null);

/// <summary>Resumen del lote de importación.</summary>
public sealed record ImportStolenReportsResult(
    int TotalReceived,
    int Created,
    int Updated,
    int Skipped,
    int ErrorCount,
    int SightingsFlagged,
    string Summary,
    IReadOnlyList<ImportStolenReportItemResult> Items,
    IReadOnlyList<string> Errors);

/// <summary>Valores posibles en <see cref="ImportStolenReportItemResult.Outcome"/>.</summary>
public static class ImportOutcomes
{
    public const string Created = "created";
    public const string UpdatedPreviousActive = "updated_previous_active";
    public const string SkippedDuplicate = "skipped_duplicate";
    public const string Error = "error";
}
