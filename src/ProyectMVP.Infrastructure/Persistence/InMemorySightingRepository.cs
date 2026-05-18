using ProyectMVP.Application.Common.Interfaces;
using ProyectMVP.Application.Sightings.SearchPlate;

namespace ProyectMVP.Infrastructure.Persistence;

/// <summary>
/// Repositorio en memoria para compilar y probar sin SQL.
/// Se reemplazará por implementación con EF Core + SQL Server.
/// </summary>
public sealed class InMemorySightingRepository : ISightingRepository
{
    private static readonly IReadOnlyList<PlateSightingDto> DemoSightings =
    [
        new PlateSightingDto(
            SightingId: 1,
            SeenAtUtc: new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            Latitude: 6.244203m,
            Longitude: -75.581212m,
            Country: "Colombia",
            City: "Medellin",
            DeviceId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            EvidenceUrl: "https://storage.example/e1.jpg",
            IsPotentialMatch: true)
    ];

    public Task<IReadOnlyList<PlateSightingDto>> GetByPlateAsync(string plate, CancellationToken cancellationToken = default)
    {
        if (plate.Equals("ABC123", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(DemoSightings);
        }

        return Task.FromResult<IReadOnlyList<PlateSightingDto>>(Array.Empty<PlateSightingDto>());
    }
}
