using Microsoft.EntityFrameworkCore;
using ProyectMVP.Application.Common.Interfaces;
using ProyectMVP.Application.Sightings.SearchPlate;

namespace ProyectMVP.Infrastructure.Persistence;

public sealed class SqlSightingRepository : ISightingRepository
{
    private readonly VehicleTheftDbContext _dbContext;

    public SqlSightingRepository(VehicleTheftDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PlateSightingDto>> GetByPlateAsync(string plate, CancellationToken cancellationToken = default)
    {
        var query =
            from vehicle in _dbContext.Vehicles.AsNoTracking()
            join sighting in _dbContext.Sightings.AsNoTracking() on vehicle.VehicleId equals sighting.VehicleId
            join country in _dbContext.Countries.AsNoTracking() on sighting.CountryId equals country.CountryId
            join city in _dbContext.Cities.AsNoTracking() on sighting.CityId equals city.CityId into cityGroup
            from city in cityGroup.DefaultIfEmpty()
            join evidence in _dbContext.SightingEvidences.AsNoTracking() on sighting.SightingId equals evidence.SightingId into evidenceGroup
            from evidence in evidenceGroup.DefaultIfEmpty()
            where vehicle.Plate == plate
            orderby sighting.SeenAtUtc descending
            select new PlateSightingDto(
                sighting.SightingId,
                sighting.SeenAtUtc,
                sighting.Latitude,
                sighting.Longitude,
                country.Name,
                city != null ? city.Name : null,
                sighting.DeviceId,
                evidence != null ? evidence.StorageUrl : null,
                sighting.IsPotentialMatch);

        return await query.ToListAsync(cancellationToken);
    }
}
