using ProyectMVP.Application.StolenReports.Import;

namespace ProyectMVP.Application.Common.Interfaces;

public interface IStolenVehicleImportRepository
{
    Task<ImportStolenReportsResult> ImportBatchAsync(
        string countryIsoCode,
        string sourceSystem,
        IReadOnlyList<StolenVehicleImportRecord> records,
        CancellationToken cancellationToken = default);
}
