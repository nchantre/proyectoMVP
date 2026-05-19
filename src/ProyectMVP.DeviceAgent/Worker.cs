using Microsoft.Extensions.Options;
using ProyectMVP.DeviceAgent.Lpr;
using ProyectMVP.DeviceAgent.Options;
using ProyectMVP.DeviceAgent.Outbox;
using ProyectMVP.DeviceAgent.Sync;

namespace ProyectMVP.DeviceAgent;

public sealed class Worker : BackgroundService
{
    private readonly ILprReader _lprReader;
    private readonly ISightingOutbox _outbox;
    private readonly ICentralApiClient _apiClient;
    private readonly DeviceAgentOptions _options;
    private readonly ILogger<Worker> _logger;

    private DateTime _lastCaptureUtc = DateTime.MinValue;
    private DateTime _lastSyncUtc = DateTime.MinValue;

    public Worker(
        ILprReader lprReader,
        ISightingOutbox outbox,
        ICentralApiClient apiClient,
        IOptions<DeviceAgentOptions> options,
        ILogger<Worker> logger)
    {
        _lprReader = lprReader;
        _outbox = outbox;
        _apiClient = apiClient;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _outbox.EnsureCreatedAsync(stoppingToken);

        _logger.LogInformation(
            "DeviceAgent activo — dispositivo {DeviceId}, captura cada {Capture}s, sync cada {Sync}s, SyncEnabled={Sync}",
            _options.DeviceId,
            _options.CaptureIntervalSeconds,
            _options.SyncIntervalSeconds,
            _options.SyncEnabled);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            if (now - _lastCaptureUtc >= TimeSpan.FromSeconds(_options.CaptureIntervalSeconds))
            {
                await CaptureAsync(stoppingToken);
                _lastCaptureUtc = now;
            }

            if (_options.SyncEnabled
                && now - _lastSyncUtc >= TimeSpan.FromSeconds(_options.SyncIntervalSeconds))
            {
                await SyncAsync(stoppingToken);
                _lastSyncUtc = now;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task CaptureAsync(CancellationToken cancellationToken)
    {
        var reading = _lprReader.Read();
        await _outbox.EnqueueAsync(reading, cancellationToken);

        var pending = await _outbox.CountPendingAsync(cancellationToken);
        _logger.LogInformation(
            "LPR → cola local: placa {Plate} @ ({Lat:F5}, {Lng:F5}) — pendientes: {Pending}",
            reading.Plate,
            reading.Latitude,
            reading.Longitude,
            pending);
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        var batch = await _outbox.GetPendingAsync(_options.SyncBatchSize, cancellationToken);
        if (batch.Count == 0)
        {
            return;
        }

        var result = await _apiClient.SendBatchAsync(batch, cancellationToken);
        if (result is null)
        {
            _logger.LogWarning(
                "Sin conexión con el central; {Count} lectura(s) permanecen en cola local.",
                batch.Count);
            return;
        }

        await _outbox.MarkSentAsync(batch.Select(b => b.Id), cancellationToken);
        var remaining = await _outbox.CountPendingAsync(cancellationToken);
        _logger.LogInformation("Cola local: {Remaining} pendiente(s)", remaining);
    }
}
