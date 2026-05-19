using ProyectMVP.Application.Common.Interfaces;

namespace ProyectMVP.Application.StolenReports.GetByPlate;

public sealed class GetStolenReportsHandler
{
    private readonly IStolenVehicleReportRepository _repository;

    public GetStolenReportsHandler(IStolenVehicleReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetStolenReportsResultDto> HandleAsync(
        string plate,
        string countryIsoCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plate))
        {
            throw new ArgumentException("La matrícula es obligatoria.", nameof(plate));
        }

        if (string.IsNullOrWhiteSpace(countryIsoCode))
        {
            throw new ArgumentException("El país del usuario no es válido.", nameof(countryIsoCode));
        }

        var normalizedPlate = plate.Trim().ToUpperInvariant();
        var reports = await _repository.GetByPlateAsync(
            normalizedPlate,
            countryIsoCode.Trim().ToUpperInvariant(),
            cancellationToken);
        return new GetStolenReportsResultDto(normalizedPlate, reports);
    }
}
