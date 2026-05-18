namespace ProyectMVP.Application.StolenReports.GetByPlate;

public sealed record StolenReportDto(
    long StolenVehicleReportId,
    string Plate,
    string Status,
    DateTime TheftDateUtc,
    string OwnerDocument,
    string Country,
    string? City,
    string? SourceSystem,
    string? Brand,
    string? VehicleClass,
    string? VehicleLine,
    string? Color,
    int? ModelYear);

public sealed record GetStolenReportsResultDto(string Plate, IReadOnlyList<StolenReportDto> Reports);
