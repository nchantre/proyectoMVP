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
            return BuildResult(
                records.Count,
                0,
                0,
                0,
                1,
                0,
                [],
                [$"País '{countryIsoCode}' no existe. Ejecute el seed SQL o cree el país antes de importar."]);
        }

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errorCount = 0;
        var items = new List<ImportStolenReportItemResult>();
        var globalErrors = new List<string>();
        var affectedVehicleIds = new HashSet<long>();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var record in records)
        {
            try
            {
                if (!ValidStatuses.Contains(record.Status))
                {
                    errorCount++;
                    items.Add(new ImportStolenReportItemResult(
                        record.Plate,
                        ImportOutcomes.Error,
                        $"Status '{record.Status}' no válido. Use ACTIVE, RECOVERED o CLOSED."));
                    continue;
                }

                var vehicle = await UpsertVehicleAsync(record, cancellationToken);
                affectedVehicleIds.Add(vehicle.VehicleId);

                var cityId = await ResolveCityIdAsync(country.CountryId, record.CityName, cancellationToken);

                var existingReport = await _dbContext.StolenVehicleReports
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        r => r.VehicleId == vehicle.VehicleId
                             && r.CountryId == country.CountryId
                             && r.TheftDateUtc == record.TheftDateUtc
                             && r.OwnerDocument == record.OwnerDocument,
                        cancellationToken);

                if (existingReport is not null)
                {
                    skipped++;
                    items.Add(new ImportStolenReportItemResult(
                        record.Plate,
                        ImportOutcomes.SkippedDuplicate,
                        $"Duplicado: ya existe reporte #{existingReport.StolenVehicleReportId} " +
                        $"(misma placa, fecha de hurto y documento del propietario).",
                        existingReport.StolenVehicleReportId));
                    continue;
                }

                long? closedReportId = null;
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
                    closedReportId = activeReport.StolenVehicleReportId;
                    updated++;
                }

                var newReport = new StolenVehicleReportEntity
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
                };

                _dbContext.StolenVehicleReports.Add(newReport);
                await _dbContext.SaveChangesAsync(cancellationToken);

                created++;
                var message = closedReportId is null
                    ? $"Reporte de hurto creado (status {record.Status})."
                    : $"Reporte creado (status {record.Status}). Hurto ACTIVE anterior #{closedReportId} cerrado como CLOSED.";

                items.Add(new ImportStolenReportItemResult(
                    record.Plate,
                    closedReportId is null ? ImportOutcomes.Created : ImportOutcomes.UpdatedPreviousActive,
                    message,
                    newReport.StolenVehicleReportId));
            }
            catch (Exception ex)
            {
                errorCount++;
                items.Add(new ImportStolenReportItemResult(
                    record.Plate,
                    ImportOutcomes.Error,
                    ex.Message));
            }
        }

        var sightingsFlagged = await RefreshPotentialMatchesAsync(affectedVehicleIds, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return BuildResult(
            records.Count,
            created,
            updated,
            skipped,
            errorCount,
            sightingsFlagged,
            items,
            globalErrors);
    }

    private static ImportStolenReportsResult BuildResult(
        int totalReceived,
        int created,
        int updated,
        int skipped,
        int errorCount,
        int sightingsFlagged,
        IReadOnlyList<ImportStolenReportItemResult> items,
        IReadOnlyList<string> globalErrors)
    {
        var summary = globalErrors.Count > 0
            ? string.Join(" ", globalErrors)
            : $"Procesados {totalReceived} registro(s): {created} creado(s), {updated} con hurto ACTIVE previo cerrado, " +
              $"{skipped} omitido(s) por duplicado, {errorCount} error(es). " +
              $"{sightingsFlagged} avistamiento(s) actualizados como posible hurto.";

        return new ImportStolenReportsResult(
            totalReceived,
            created,
            updated,
            skipped,
            errorCount,
            sightingsFlagged,
            summary,
            items,
            globalErrors);
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
