using ProyectMVP.Application.Common.Interfaces;

namespace ProyectMVP.Application.Sightings.RegisterSighting;

public sealed class RegisterSightingHandler
{
    private readonly IDeviceAuthService _deviceAuthService;
    private readonly ISightingRegistrationRepository _registrationRepository;
    private readonly IMessageBus _messageBus;

    public RegisterSightingHandler(
        IDeviceAuthService deviceAuthService,
        ISightingRegistrationRepository registrationRepository,
        IMessageBus messageBus)
    {
        _deviceAuthService = deviceAuthService;
        _registrationRepository = registrationRepository;
        _messageBus = messageBus;
    }

    public async Task<RegisterSightingBatchResult> HandleAsync(
        Guid deviceId,
        string apiKey,
        IReadOnlyList<RegisterSightingItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("Debe enviar al menos un avistamiento.", nameof(items));
        }

        if (!await _deviceAuthService.ValidateDeviceAsync(deviceId, apiKey, cancellationToken))
        {
            throw new UnauthorizedAccessException("Dispositivo o API Key no válidos.");
        }

        var normalizedItems = items
            .Select(i => i with { Plate = i.Plate.Trim().ToUpperInvariant() })
            .ToList();

        var result = await _registrationRepository.RegisterBatchAsync(deviceId, normalizedItems, cancellationToken);

        foreach (var item in result.Items.Where(x => !x.WasDuplicate))
        {
            await _messageBus.PublishAsync(
                "sighting.registered",
                new { item.SightingId, item.Plate, item.IsPotentialMatch },
                cancellationToken);
        }

        return result;
    }
}
