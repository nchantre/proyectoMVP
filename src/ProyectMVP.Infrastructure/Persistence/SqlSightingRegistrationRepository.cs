using Microsoft.EntityFrameworkCore;
using ProyectMVP.Application.Common.Interfaces;
using ProyectMVP.Application.Sightings.RegisterSighting;
using ProyectMVP.Infrastructure.Persistence.Entities;

namespace ProyectMVP.Infrastructure.Persistence;

public sealed class SqlSightingRegistrationRepository : ISightingRegistrationRepository
{
    private readonly VehicleTheftDbContext _dbContext;

    public SqlSightingRegistrationRepository(VehicleTheftDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RegisterSightingBatchResult> RegisterBatchAsync(
        Guid deviceId,
        IReadOnlyList<RegisterSightingItem> items,
        CancellationToken cancellationToken = default)
    {
        var device = await _dbContext.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId, cancellationToken)
            ?? throw new InvalidOperationException($"Dispositivo {deviceId} no encontrado.");

        var results = new List<RegisteredSightingResult>();
        var accepted = 0;
        var duplicates = 0;

        foreach (var item in items)
        {
            var vehicle = await GetOrCreateVehicleAsync(item.Plate, cancellationToken);

            var existing = await _dbContext.Sightings
                .AsNoTracking()
                .AnyAsync(
                    s => s.DeviceId == deviceId
                         && s.VehicleId == vehicle.VehicleId
                         && s.SeenAtUtc == item.SeenAtUtc,
                    cancellationToken);

            if (existing)
            {
                duplicates++;
                var existingId = await _dbContext.Sightings
                    .Where(s => s.DeviceId == deviceId && s.VehicleId == vehicle.VehicleId && s.SeenAtUtc == item.SeenAtUtc)
                    .Select(s => s.SightingId)
                    .FirstAsync(cancellationToken);

                var existingMatch = await _dbContext.Sightings
                    .Where(s => s.SightingId == existingId)
                    .Select(s => s.IsPotentialMatch)
                    .FirstAsync(cancellationToken);

                results.Add(new RegisteredSightingResult(existingId, item.Plate, existingMatch, WasDuplicate: true));
                continue;
            }

            var isPotentialMatch = await _dbContext.StolenVehicleReports
                .AnyAsync(
                    r => r.VehicleId == vehicle.VehicleId && r.Status == "ACTIVE",
                    cancellationToken);

            var sighting = new SightingEntity
            {
                DeviceId = deviceId,
                VehicleId = vehicle.VehicleId,
                SeenAtUtc = item.SeenAtUtc,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                CountryId = device.CountryId,
                CityId = device.CityId,
                Confidence = item.Confidence,
                IsPotentialMatch = isPotentialMatch,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.Sightings.Add(sighting);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(item.EvidenceUrl))
            {
                _dbContext.SightingEvidences.Add(new SightingEvidenceEntity
                {
                    SightingId = sighting.SightingId,
                    EvidenceType = item.EvidenceType,
                    StorageUrl = item.EvidenceUrl.Trim(),
                    CapturedAtUtc = item.SeenAtUtc
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            accepted++;
            results.Add(new RegisteredSightingResult(
                sighting.SightingId,
                item.Plate,
                isPotentialMatch,
                WasDuplicate: false));
        }

        device.LastSeenAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterSightingBatchResult(accepted, duplicates, results);
    }

    private async Task<VehicleEntity> GetOrCreateVehicleAsync(string plate, CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Plate == plate, cancellationToken);

        if (vehicle is not null)
        {
            return vehicle;
        }

        vehicle = new VehicleEntity { Plate = plate };
        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return vehicle;
    }
}
