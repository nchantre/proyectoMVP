using ProyectMVP.Application.StolenReports.GetByPlate;

namespace ProyectMVP.Application.Common.Interfaces;

public interface IStolenVehicleReportRepository
{
    Task<IReadOnlyList<StolenReportDto>> GetByPlateAsync(
        string plate,
        string countryIsoCode,
        CancellationToken cancellationToken = default);
}
