using ProyectMVP.Application.Common.Interfaces;

namespace ProyectMVP.Application.Sightings.SearchPlate;

public sealed class SearchPlateHandler
{
    private readonly ISightingRepository _sightingRepository;

    public SearchPlateHandler(ISightingRepository sightingRepository)
    {
        _sightingRepository = sightingRepository;
    }

    public async Task<SearchPlateResultDto> HandleAsync(string plate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plate))
        {
            throw new ArgumentException("La matrícula es obligatoria.", nameof(plate));
        }

        var normalizedPlate = plate.Trim().ToUpperInvariant();
        var sightings = await _sightingRepository.GetByPlateAsync(normalizedPlate, cancellationToken);
        return new SearchPlateResultDto(normalizedPlate, sightings);
    }
}
