namespace ProyectMVP.Application.Sightings.RegisterSighting;

public sealed record RegisterSightingItem(
    string Plate,
    DateTime SeenAtUtc,
    decimal Latitude,
    decimal Longitude,
    decimal? Confidence,
    string? EvidenceUrl,
    string EvidenceType = "IMAGE");
