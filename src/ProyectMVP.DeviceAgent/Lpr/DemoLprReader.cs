using Microsoft.Extensions.Options;
using ProyectMVP.DeviceAgent.Models;
using ProyectMVP.DeviceAgent.Options;

namespace ProyectMVP.DeviceAgent.Lpr;

/// <summary>
/// Simula el módulo LPR local (API del fabricante en la calle).
/// </summary>
public sealed class DemoLprReader : ILprReader
{
    private readonly DeviceAgentOptions _options;
    private readonly Random _random = new();

    public DemoLprReader(IOptions<DeviceAgentOptions> options)
    {
        _options = options.Value;
    }

    public LprReading Read()
    {
        var plates = _options.Plates.Length > 0 ? _options.Plates : ["ABC123"];
        var plate = plates[_random.Next(plates.Length)].Trim().ToUpperInvariant();
        var seenAt = DateTime.UtcNow;

        var lat = (decimal)(_options.BaseLatitude + (_random.NextDouble() * 2 - 1) * _options.LocationJitterDegrees);
        var lng = (decimal)(_options.BaseLongitude + (_random.NextDouble() * 2 - 1) * _options.LocationJitterDegrees);
        var confidence = (decimal)(85 + _random.NextDouble() * 14);

        var evidenceUrl = _options.EvidenceUrlTemplate
            .Replace("{plate}", plate, StringComparison.OrdinalIgnoreCase)
            .Replace("{timestamp}", seenAt.ToString("yyyyMMddHHmmss"), StringComparison.OrdinalIgnoreCase);

        return new LprReading(plate, seenAt, lat, lng, confidence, evidenceUrl);
    }
}
