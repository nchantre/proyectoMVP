using System.ComponentModel;

namespace ProyectMVP.Api.Contracts;

/// <summary>Solicitud de importación de reportes de hurto.</summary>
public sealed class ImportStolenReportsRequest
{
    /// <summary>json = usar <see cref="Records"/>; csv = usar <see cref="Payload"/>.</summary>
    [DefaultValue("json")]
    public string Format { get; set; } = "json";

    /// <summary>Código ISO del país (debe existir en BD), ej. CO.</summary>
    [DefaultValue("CO")]
    public string CountryIsoCode { get; set; } = "CO";

    /// <summary>Sistema origen del lote, ej. POLICIA_CO_BATCH.</summary>
    public string SourceSystem { get; set; } = string.Empty;

    /// <summary>Texto CSV completo cuando format=csv.</summary>
    public string? Payload { get; set; }

    /// <summary>Filas a importar cuando format=json.</summary>
    public List<ImportStolenReportItem>? Records { get; set; }
}

public sealed class ImportStolenReportItem
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
