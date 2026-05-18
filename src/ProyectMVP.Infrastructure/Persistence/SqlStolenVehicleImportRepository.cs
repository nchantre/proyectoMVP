using Microsoft.EntityFrameworkCore;
using ProyectMVP.Application.Common.Interfaces;
using ProyectMVP.Application.StolenReports.Import;
using ProyectMVP.Infrastructure.Persistence.Entities;

namespace ProyectMVP.Infrastructure.Persistence;

public sealed class SqlStolenVehicleImportRepository : IStolenVehicleImportRepository
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACTIVE", "RECOVERED", "CLOSED"
    };

    private readonly VehicleTheftDbContext _dbContext;

    public SqlStolenVehicleImportRepository(VehicleTheftDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ImportStolenReportsResult> ImportBatchAsync(
        string countryIsoCode,
        string sourceSystem,
        IReadOnlyList<StolenVehicleImportRecord> records,
        CancellationToken cancellationToken = default)
    {
        var country = await _dbContext.Countries
            .FirstOrDefaultAsync(c => c.IsoCode == countryIsoCode, cancellationToken);

        if (country is null)
        {
            return new ImportStolenReportsResult(0, 0, 0, 0, [$"País '{countryIsoCode}' no existe en el sistema."]);
        }

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();
        var affectedVehicleIds = new HashSet<long>();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var record in records)
        {
            try
            {
                if (!ValidStatuses.Contains(record.Status))
                {
                    errors.Add($"{record.Plate}: status '{record.Status}' no válido.");
                    continue;
                }

                var vehicle = await UpsertVehicleAsync(record, cancellationToken);
                affectedVehicleIds.Add(vehicle.VehicleId);

                var cityId = await ResolveCityIdAsync(country.CountryId, record.CityName, cancellationToken);

                var existingReport = await _dbContext.StolenVehicleReports
                    .FirstOrDefaultAsync(
                        r => r.VehicleId == vehicle.VehicleId
                             && r.CountryId == country.CountryId
                             && r.TheftDateUtc == record.TheftDateUtc
                             && r.OwnerDocument == record.OwnerDocument,
                        cancellationToken);

                if (existingReport is not null)
                {
                    skipped++;
                    continue;
                }

                var activeReport = await _dbContext.StolenVehicleReports
                    .FirstOrDefaultAsync(
                        r => r.VehicleId == vehicle.VehicleId
                             && r.CountryId == country.CountryId
                             && r.Status == "ACTIVE",
                        cancellationToken);

                if (activeReport is not null && record.Status == "ACTIVE")
                {
                    activeReport.Status = "CLOSED";
                    activeReport.UpdatedAtUtc = DateTime.UtcNow;
                    updated++;
                }

                _dbContext.StolenVehicleReports.Add(new StolenVehicleReportEntity
                {
                    VehicleId = vehicle.VehicleId,
                    CountryId = country.CountryId,
                    CityId = cityId,
                    OwnerDocument = record.OwnerDocument,
                    TheftDateUtc = record.TheftDateUtc,
                    Status = record.Status,
                    SourceSystem = sourceSystem,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });

                created++;
            }
            catch (Exception ex)
            {
                errors.Add($"{record.Plate}: {ex.Message}");
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var sightingsFlagged = await RefreshPotentialMatchesAsync(affectedVehicleIds, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ImportStolenReportsResult(created, updated, skipped, sightingsFlagged, errors);
    }

    private async Task<VehicleEntity> UpsertVehicleAsync(
        StolenVehicleImportRecord record,
        CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Plate == record.Plate, cancellationToken);

        if (vehicle is null)
        {
            vehicle = new VehicleEntity
            {
                Plate = record.Plate,
                Brand = record.Brand,
                VehicleClass = record.VehicleClass,
                VehicleLine = record.VehicleLine,
                Color = record.Color,
                ModelYear = record.ModelYear
            };
            _dbContext.Vehicles.Add(vehicle);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return vehicle;
        }

        var changed = false;
        if (record.Brand is not null && vehicle.Brand != record.Brand) { vehicle.Brand = record.Brand; changed = true; }
        if (record.VehicleClass is not null && vehicle.VehicleClass != record.VehicleClass) { vehicle.VehicleClass = record.VehicleClass; changed = true; }
        if (record.VehicleLine is not null && vehicle.VehicleLine != record.VehicleLine) { vehicle.VehicleLine = record.VehicleLine; changed = true; }
        if (record.Color is not null && vehicle.Color != record.Color) { vehicle.Color = record.Color; changed = true; }
        if (record.ModelYear is not null && vehicle.ModelYear != record.ModelYear) { vehicle.ModelYear = record.ModelYear; changed = true; }

        if (changed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return vehicle;
    }

    private async Task<int?> ResolveCityIdAsync(
        int countryId,
        string? cityName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cityName))
        {
            return null;
        }

        var normalized = cityName.Trim();
        var city = await _dbContext.Cities
            .FirstOrDefaultAsync(
                c => c.CountryId == countryId && c.Name == normalized,
                cancellationToken);

        if (city is not null)
        {
            return city.CityId;
        }

        city = new CityEntity { CountryId = countryId, Name = normalized };
        _dbContext.Cities.Add(city);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return city.CityId;
    }

    private async Task<int> RefreshPotentialMatchesAsync(
        HashSet<long> vehicleIds,
        CancellationToken cancellationToken)
    {
        if (vehicleIds.Count == 0)
        {
            return 0;
        }

        var activeVehicleIds = await _dbContext.StolenVehicleReports
            .AsNoTracking()
            .Where(r => vehicleIds.Contains(r.VehicleId) && r.Status == "ACTIVE")
            .Select(r => r.VehicleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var activeSet = activeVehicleIds.ToHashSet();
        var flagged = 0;

        var sightings = await _dbContext.Sightings
            .Where(s => vehicleIds.Contains(s.VehicleId))
            .ToListAsync(cancellationToken);

        foreach (var sighting in sightings)
        {
            var shouldMatch = activeSet.Contains(sighting.VehicleId);
            if (sighting.IsPotentialMatch == shouldMatch)
            {
                continue;
            }

            sighting.IsPotentialMatch = shouldMatch;
            flagged++;
        }

        if (flagged > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return flagged;
    }
}
