using ProyectMVP.Application.StolenReports.GetByPlate;

namespace ProyectMVP.Application.Common.Interfaces;

public interface IStolenVehicleReportRepository
{
    Task<IReadOnlyList<StolenReportDto>> GetByPlateAsync(
        string plate,
        int countryId,
        CancellationToken cancellationToken = default);
}
