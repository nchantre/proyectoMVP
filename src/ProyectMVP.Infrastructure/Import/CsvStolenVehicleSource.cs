using System.Globalization;
using ProyectMVP.Application.Common.Interfaces;
using ProyectMVP.Application.StolenReports.Import;

namespace ProyectMVP.Infrastructure.Import;

/// <summary>
/// Formato CSV (simula exportación legacy de otro país).
/// Columnas: plate,theft_date,city,owner_document,brand,class,line,color,model_year,status
/// </summary>
public sealed class CsvStolenVehicleSource : IStolenVehicleSource
{
    public string Format => "csv";

    public IReadOnlyList<StolenVehicleImportRecord> Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload CSV vacío.");
        }

        var lines = payload.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            throw new ArgumentException("CSV debe incluir encabezado y al menos una fila.");
        }

        var header = lines[0].Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
        var records = new List<StolenVehicleImportRecord>();

        for (var i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 4)
            {
                continue;
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < header.Length && c < cols.Length; c++)
            {
                map[header[c]] = cols[c].Trim();
            }

            var plate = GetRequired(map, "plate");
            var theftDate = DateTime.Parse(
                GetRequired(map, "theft_date"),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            int? modelYear = map.TryGetValue("model_year", out var yearText) && int.TryParse(yearText, out var y)
                ? y
                : null;

            records.Add(new StolenVehicleImportRecord(
                plate.ToUpperInvariant(),
                theftDate,
                GetRequired(map, "owner_document"),
                map.GetValueOrDefault("city"),
                map.GetValueOrDefault("status")?.ToUpperInvariant() ?? "ACTIVE",
                map.GetValueOrDefault("brand"),
                map.GetValueOrDefault("class"),
                map.GetValueOrDefault("line"),
                map.GetValueOrDefault("color"),
                modelYear));
        }

        return records;
    }

    private static string GetRequired(IReadOnlyDictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"CSV: columna '{key}' es obligatoria.");
        }

        return value;
    }
}
