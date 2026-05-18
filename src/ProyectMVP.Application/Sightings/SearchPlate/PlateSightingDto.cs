namespace ProyectMVP.Application.Sightings.SearchPlate;

public sealed record PlateSightingDto(
    long SightingId,
    DateTime SeenAtUtc,
    decimal Latitude,
    decimal Longitude,
    string Country,
    string? City,
    Guid DeviceId,
    string? EvidenceUrl,
    bool IsPotentialMatch);

public sealed record SearchPlateResultDto(string Plate, IReadOnlyList<PlateSightingDto> Sightings);
