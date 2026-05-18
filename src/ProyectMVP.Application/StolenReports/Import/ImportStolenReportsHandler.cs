using ProyectMVP.Application.Common.Interfaces;

namespace ProyectMVP.Application.StolenReports.Import;

public sealed class ImportStolenReportsHandler
{
    private readonly IEnumerable<IStolenVehicleSource> _sources;
    private readonly IStolenVehicleImportRepository _importRepository;

    public ImportStolenReportsHandler(
        IEnumerable<IStolenVehicleSource> sources,
        IStolenVehicleImportRepository importRepository)
    {
        _sources = sources;
        _importRepository = importRepository;
    }

    public async Task<ImportStolenReportsResult> HandleAsync(
        string format,
        string countryIsoCode,
        string sourceSystem,
        string? payload,
        IReadOnlyList<StolenVehicleImportRecord>? records,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(countryIsoCode))
        {
            throw new ArgumentException("CountryIsoCode es obligatorio.", nameof(countryIsoCode));
        }

        if (string.IsNullOrWhiteSpace(sourceSystem))
        {
            throw new ArgumentException("SourceSystem es obligatorio.", nameof(sourceSystem));
        }

        var normalizedFormat = (format ?? "json").Trim().ToLowerInvariant();
        var parsedRecords = normalizedFormat switch
        {
            "json" => records is { Count: > 0 }
                ? records
                : throw new ArgumentException("Para format=json envíe el arreglo records.", nameof(records)),
            _ => ResolveSource(normalizedFormat).Parse(payload
                ?? throw new ArgumentException($"Para format={normalizedFormat} envíe payload.", nameof(payload)))
        };

        if (parsedRecords.Count == 0)
        {
            return new ImportStolenReportsResult(0, 0, 0, 0, ["No hay registros para importar."]);
        }

        return await _importRepository.ImportBatchAsync(
            countryIsoCode.Trim().ToUpperInvariant(),
            sourceSystem.Trim(),
            parsedRecords,
            cancellationToken);
    }

    private IStolenVehicleSource ResolveSource(string format)
    {
        var source = _sources.FirstOrDefault(s =>
            string.Equals(s.Format, format, StringComparison.OrdinalIgnoreCase));

        return source ?? throw new ArgumentException($"Formato no soportado: {format}. Use json o csv.");
    }
}
