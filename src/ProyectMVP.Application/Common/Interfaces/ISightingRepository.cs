using ProyectMVP.Application.Sightings.SearchPlate;

namespace ProyectMVP.Application.Common.Interfaces;

public interface ISightingRepository
{
    Task<IReadOnlyList<PlateSightingDto>> GetByPlateAsync(string plate, CancellationToken cancellationToken = default);
}
