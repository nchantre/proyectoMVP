using ProyectMVP.Application.Common.Interfaces;

namespace ProyectMVP.Application.Analytics.Hotspots;

public sealed class GetHotspotsHandler
{
    private readonly IAnalyticsRepository _analyticsRepository;

    public GetHotspotsHandler(IAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public Task<GetHotspotsResultDto> HandleAsync(
        int countryId,
        string countryIsoCode,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? category,
        CancellationToken cancellationToken = default)
    {
        if (countryId <= 0)
        {
            throw new ArgumentException("El país del usuario no es válido.", nameof(countryId));
        }

        var to = toUtc ?? DateTime.UtcNow;
        var from = fromUtc ?? to.AddDays(-30);

        if (from > to)
        {
            throw new ArgumentException("'from' no puede ser posterior a 'to'.");
        }

        var normalizedCategory = string.IsNullOrWhiteSpace(category)
            ? HotspotCategories.All
            : category.Trim().ToLowerInvariant();

        if (normalizedCategory is not (
            HotspotCategories.All or
            HotspotCategories.Theft or
            HotspotCategories.Sighting or
            HotspotCategories.PotentialMatch))
        {
            throw new ArgumentException(
                $"Categoría no válida: {category}. Use: all, theft, sighting, potential_match.");
        }

        return _analyticsRepository.GetHotspotsAsync(
            countryId,
            countryIsoCode,
            from,
            to,
            normalizedCategory == HotspotCategories.All ? null : normalizedCategory,
            cancellationToken);
    }
}
