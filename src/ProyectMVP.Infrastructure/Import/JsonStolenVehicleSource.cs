using System.Text.Json;
using ProyectMVP.Application.Common.Interfaces;
using ProyectMVP.Application.StolenReports.Import;

namespace ProyectMVP.Infrastructure.Import;

/// <summary>
/// Formato JSON estándar (ej. API REST de policía).
/// </summary>
public sealed class JsonStolenVehicleSource : IStolenVehicleSource
{
    public string Format => "json";

    public IReadOnlyList<StolenVehicleImportRecord> Parse(string payload)
    {
        var items = JsonSerializer.Deserialize<List<JsonImportItem>>(payload, JsonOptions)
            ?? throw new ArgumentException("JSON de importación inválido.");

        return items.Select(Map).ToList();
    }

    internal static StolenVehicleImportRecord Map(JsonImportItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Plate))
        {
            throw new ArgumentException("Cada registro debe incluir plate.");
        }

        if (string.IsNullOrWhiteSpace(item.OwnerDocument))
        {
            throw new ArgumentException($"Registro {item.Plate}: ownerDocument es obligatorio.");
        }

        return new StolenVehicleImportRecord(
            item.Plate.Trim().ToUpperInvariant(),
            item.TheftDateUtc,
            item.OwnerDocument.Trim(),
            string.IsNullOrWhiteSpace(item.CityName) ? null : item.CityName.Trim(),
            string.IsNullOrWhiteSpace(item.Status) ? "ACTIVE" : item.Status.Trim().ToUpperInvariant(),
            item.Brand,
            item.VehicleClass,
            item.VehicleLine,
            item.Color,
            item.ModelYear);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal sealed class JsonImportItem
    {
        public string Plate { get; set; } = string.Empty;
        public DateTime TheftDateUtc { get; set; }
        public string OwnerDocument { get; set; } = string.Empty;
        public string? CityName { get; set; }
        public string? Status { get; set; }
        public string? Brand { get; set; }
        public string? VehicleClass { get; set; }
        public string? VehicleLine { get; set; }
        public string? Color { get; set; }
        public int? ModelYear { get; set; }
    }
}
