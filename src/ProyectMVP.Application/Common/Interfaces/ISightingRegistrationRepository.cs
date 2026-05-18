using ProyectMVP.Application.Sightings.RegisterSighting;

namespace ProyectMVP.Application.Common.Interfaces;

public interface ISightingRegistrationRepository
{
    Task<RegisterSightingBatchResult> RegisterBatchAsync(
        Guid deviceId,
        IReadOnlyList<RegisterSightingItem> items,
        CancellationToken cancellationToken = default);
}
