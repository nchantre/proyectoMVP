using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ProyectMVP.Application.Common.Interfaces;
using ProyectMVP.Infrastructure.Persistence;

namespace ProyectMVP.Infrastructure.Security;

public sealed class DeviceAuthService : IDeviceAuthService
{
    private const string DemoHashPlaceholder = "SHA256_DEMO_KEY_HASH";
    private const string DemoApiKey = "DEMO_KEY";

    private readonly VehicleTheftDbContext _dbContext;

    public DeviceAuthService(VehicleTheftDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ValidateDeviceAsync(Guid deviceId, string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        var device = await _dbContext.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId, cancellationToken);

        if (device is null || !device.IsActive)
        {
            return false;
        }

        if (device.ApiKeyHash == DemoHashPlaceholder
            && string.Equals(apiKey, DemoApiKey, StringComparison.Ordinal))
        {
            return true;
        }

        var hash = ComputeSha256Hex(apiKey);
        return string.Equals(device.ApiKeyHash, hash, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
