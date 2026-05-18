namespace ProyectMVP.Application.Common.Interfaces;

public interface IDeviceAuthService
{
    Task<bool> ValidateDeviceAsync(Guid deviceId, string apiKey, CancellationToken cancellationToken = default);
}
