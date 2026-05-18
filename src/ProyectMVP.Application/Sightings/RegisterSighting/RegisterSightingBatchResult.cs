namespace ProyectMVP.Application.Sightings.RegisterSighting;

public sealed record RegisteredSightingResult(
    long SightingId,
    string Plate,
    bool IsPotentialMatch,
    bool WasDuplicate);

public sealed record RegisterSightingBatchResult(
    int Accepted,
    int Duplicates,
    IReadOnlyList<RegisteredSightingResult> Items);
