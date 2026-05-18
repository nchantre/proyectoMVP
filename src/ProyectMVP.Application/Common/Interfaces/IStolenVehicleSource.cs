using ProyectMVP.Application.StolenReports.Import;

namespace ProyectMVP.Application.Common.Interfaces;

/// <summary>
/// Adaptador por país/formato para normalizar datos de hurtos externos.
/// </summary>
public interface IStolenVehicleSource
{
    string Format { get; }

    IReadOnlyList<StolenVehicleImportRecord> Parse(string payload);
}
