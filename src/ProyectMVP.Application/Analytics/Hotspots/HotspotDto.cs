namespace ProyectMVP.Application.Analytics.Hotspots;

/// <summary>Punto agregado para mapa de calor / BI.</summary>
public sealed record HotspotDto(
    string Category,
    string Label,
    string? City,
    decimal Latitude,
    decimal Longitude,
    int Count,
    double Intensity);

/// <summary>Resultado de consulta de hotspots.</summary>
public sealed record GetHotspotsResultDto(
    string CountryIsoCode,
    DateTime FromUtc,
    DateTime ToUtc,
    string? CategoryFilter,
    string Summary,
    IReadOnlyList<HotspotDto> Hotspots);

public static class HotspotCategories
{
    public const string Theft = "theft";
    public const string Sighting = "sighting";
    public const string PotentialMatch = "potential_match";
    public const string All = "all";
}
