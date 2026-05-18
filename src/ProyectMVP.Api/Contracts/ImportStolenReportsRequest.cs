namespace ProyectMVP.Api.Contracts;

public sealed class ImportStolenReportsRequest
{
    /// <summary>json (records en body) o csv (payload texto).</summary>
    public string Format { get; set; } = "json";

    public string CountryIsoCode { get; set; } = "CO";

    public string SourceSystem { get; set; } = string.Empty;

    /// <summary>Contenido CSV cuando format=csv.</summary>
    public string? Payload { get; set; }

    public List<ImportStolenReportItem>? Records { get; set; }
}

public sealed class ImportStolenReportItem
{
    public string Plate { get; set; } = string.Empty;
    public DateTime TheftDateUtc { get; set; }
    public string OwnerDocument { get; set; } = string.Empty;
    public string? CityName { get; set; }
    public string? Status { get; set; }
    public string? Brand { get; set; }
    public string? VehicleClass { get; set; }
    public string? VehicleLine { get; set; }
    public string? Color { get; set; }
    public int? ModelYear { get; set; }
}
