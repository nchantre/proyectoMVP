using ProyectMVP.DeviceAgent.Models;

namespace ProyectMVP.DeviceAgent.Outbox;

public interface ISightingOutbox
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);

    Task EnqueueAsync(LprReading reading, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingSighting>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task MarkSentAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);

    Task<int> CountPendingAsync(CancellationToken cancellationToken = default);
}
