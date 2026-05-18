namespace ProyectMVP.Infrastructure.Persistence.Entities;

public sealed class StolenVehicleReportEntity
{
    public long StolenVehicleReportId { get; set; }
    public long VehicleId { get; set; }
    public int CountryId { get; set; }
    public int? CityId { get; set; }
    public string OwnerDocument { get; set; } = string.Empty;
    public DateTime TheftDateUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SourceSystem { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
