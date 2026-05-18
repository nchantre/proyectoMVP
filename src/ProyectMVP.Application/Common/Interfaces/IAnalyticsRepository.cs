using ProyectMVP.Application.Analytics.Hotspots;

namespace ProyectMVP.Application.Common.Interfaces;

public interface IAnalyticsRepository
{
    Task<GetHotspotsResultDto> GetHotspotsAsync(
        int countryId,
        string countryIsoCode,
        DateTime fromUtc,
        DateTime toUtc,
        string? category,
        CancellationToken cancellationToken = default);
}
