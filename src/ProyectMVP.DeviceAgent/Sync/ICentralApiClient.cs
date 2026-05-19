using ProyectMVP.DeviceAgent.Models;

namespace ProyectMVP.DeviceAgent.Sync;

public interface ICentralApiClient
{
    Task<RegisterSightingBatchResponse?> SendBatchAsync(
        IReadOnlyList<PendingSighting> items,
        CancellationToken cancellationToken = default);
}
