using Microsoft.EntityFrameworkCore;
using ProyectMVP.Application.Common.Interfaces;
using ProyectMVP.Application.StolenReports.GetByPlate;

namespace ProyectMVP.Infrastructure.Persistence;

public sealed class SqlStolenVehicleReportRepository : IStolenVehicleReportRepository
{
    private readonly VehicleTheftDbContext _dbContext;

    public SqlStolenVehicleReportRepository(VehicleTheftDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<StolenReportDto>> GetByPlateAsync(
        string plate,
        string countryIsoCode,
        CancellationToken cancellationToken = default)
    {
        var query =
            from vehicle in _dbContext.Vehicles.AsNoTracking()
            join report in _dbContext.StolenVehicleReports.AsNoTracking() on vehicle.VehicleId equals report.VehicleId
            join country in _dbContext.Countries.AsNoTracking() on report.CountryId equals country.CountryId
            join city in _dbContext.Cities.AsNoTracking() on report.CityId equals city.CityId into cityGroup
            from city in cityGroup.DefaultIfEmpty()
            where vehicle.Plate == plate && country.IsoCode == countryIsoCode
            orderby report.TheftDateUtc descending
            select new StolenReportDto(
                report.StolenVehicleReportId,
                vehicle.Plate,
                report.Status,
                report.TheftDateUtc,
                report.OwnerDocument,
                country.Name,
                city != null ? city.Name : null,
                report.SourceSystem,
                vehicle.Brand,
                vehicle.VehicleClass,
                vehicle.VehicleLine,
                vehicle.Color,
                vehicle.ModelYear);

        return await query.ToListAsync(cancellationToken);
    }
}
