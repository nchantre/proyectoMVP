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

public sealed record ImportStolenReportsResult(
    int Created,
    int Updated,
    int Skipped,
    int SightingsFlagged,
    IReadOnlyList<string> Errors);
