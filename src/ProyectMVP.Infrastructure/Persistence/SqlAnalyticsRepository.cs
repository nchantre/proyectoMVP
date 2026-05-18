using Microsoft.EntityFrameworkCore;
using ProyectMVP.Application.Analytics.Hotspots;
using ProyectMVP.Application.Common.Interfaces;

namespace ProyectMVP.Infrastructure.Persistence;

public sealed class SqlAnalyticsRepository : IAnalyticsRepository
{
    private readonly VehicleTheftDbContext _dbContext;

    public SqlAnalyticsRepository(VehicleTheftDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetHotspotsResultDto> GetHotspotsAsync(
        int countryId,
        string countryIsoCode,
        DateTime fromUtc,
        DateTime toUtc,
        string? category,
        CancellationToken cancellationToken = default)
    {
        var hotspots = new List<HotspotDto>();

        if (category is null or HotspotCategories.Theft)
        {
            hotspots.AddRange(await GetTheftHotspotsAsync(countryId, fromUtc, toUtc, cancellationToken));
        }

        if (category is null or HotspotCategories.Sighting)
        {
            hotspots.AddRange(await GetSightingHotspotsAsync(countryId, fromUtc, toUtc, potentialMatchOnly: false, cancellationToken));
        }

        if (category is null or HotspotCategories.PotentialMatch)
        {
            hotspots.AddRange(await GetSightingHotspotsAsync(countryId, fromUtc, toUtc, potentialMatchOnly: true, cancellationToken));
        }

        ApplyIntensity(hotspots);

        var categoryLabel = category ?? HotspotCategories.All;
        var summary = hotspots.Count == 0
            ? $"Sin hotspots para {countryIsoCode} entre {fromUtc:yyyy-MM-dd} y {toUtc:yyyy-MM-dd} (categoría: {categoryLabel})."
            : $"Se encontraron {hotspots.Count} hotspot(s) en {countryIsoCode} " +
              $"({fromUtc:yyyy-MM-dd} → {toUtc:yyyy-MM-dd}, categoría: {categoryLabel}).";

        return new GetHotspotsResultDto(
            countryIsoCode,
            fromUtc,
            toUtc,
            category,
            summary,
            hotspots.OrderByDescending(h => h.Count).ToList());
    }

    private async Task<List<HotspotDto>> GetTheftHotspotsAsync(
        int countryId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var theftByCity = await (
            from report in _dbContext.StolenVehicleReports.AsNoTracking()
            join city in _dbContext.Cities.AsNoTracking() on report.CityId equals city.CityId into cityGroup
            from city in cityGroup.DefaultIfEmpty()
            where report.CountryId == countryId
                  && report.TheftDateUtc >= fromUtc
                  && report.TheftDateUtc <= toUtc
            group report by new { report.CityId, CityName = city != null ? city.Name : "Sin ciudad" }
            into g
            select new
            {
                g.Key.CityId,
                g.Key.CityName,
                Count = g.Count()
            }).ToListAsync(cancellationToken);

        var result = new List<HotspotDto>();

        foreach (var item in theftByCity)
        {
            var coords = await ResolveCityCoordinatesAsync(countryId, item.CityId, cancellationToken);
            result.Add(new HotspotDto(
                HotspotCategories.Theft,
                $"Hurtos — {item.CityName}",
                item.CityName,
                coords.Latitude,
                coords.Longitude,
                item.Count,
                0));
        }

        return result;
    }

    private async Task<List<HotspotDto>> GetSightingHotspotsAsync(
        int countryId,
        DateTime fromUtc,
        DateTime toUtc,
        bool potentialMatchOnly,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Sightings.AsNoTracking()
            .Where(s => s.CountryId == countryId
                        && s.SeenAtUtc >= fromUtc
                        && s.SeenAtUtc <= toUtc);

        if (potentialMatchOnly)
        {
            query = query.Where(s => s.IsPotentialMatch);
        }

        var grouped = await query
            .GroupBy(s => new
            {
                GridLat = Math.Round(s.Latitude, 2),
                GridLng = Math.Round(s.Longitude, 2)
            })
            .Select(g => new
            {
                g.Key.GridLat,
                g.Key.GridLng,
                Count = g.Count(),
                CityName = g.Select(s => s.CityId).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var category = potentialMatchOnly ? HotspotCategories.PotentialMatch : HotspotCategories.Sighting;
        var prefix = potentialMatchOnly ? "Posible hurto" : "Avistamientos";

        return grouped.Select(g => new HotspotDto(
            category,
            $"{prefix} ({g.GridLat}, {g.GridLng})",
            null,
            g.GridLat,
            g.GridLng,
            g.Count,
            0)).ToList();
    }

    private async Task<(decimal Latitude, decimal Longitude)> ResolveCityCoordinatesAsync(
        int countryId,
        int? cityId,
        CancellationToken cancellationToken)
    {
        if (cityId is not null)
        {
            var avg = await _dbContext.Sightings.AsNoTracking()
                .Where(s => s.CountryId == countryId && s.CityId == cityId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Lat = g.Average(s => s.Latitude),
                    Lng = g.Average(s => s.Longitude)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (avg is not null)
            {
                return (avg.Lat, avg.Lng);
            }
        }

        return (6.244203m, -75.581212m);
    }

    private static void ApplyIntensity(List<HotspotDto> hotspots)
    {
        if (hotspots.Count == 0)
        {
            return;
        }

        var max = hotspots.Max(h => h.Count);
        if (max <= 0)
        {
            return;
        }

        for (var i = 0; i < hotspots.Count; i++)
        {
            var h = hotspots[i];
            hotspots[i] = h with { Intensity = Math.Round((double)h.Count / max, 2) };
        }
    }
}
