using Microsoft.Extensions.DependencyInjection;
using ProyectMVP.Application.Analytics.Hotspots;
using ProyectMVP.Application.Auth.Login;
using ProyectMVP.Application.Sightings.RegisterSighting;
using ProyectMVP.Application.Sightings.SearchPlate;
using ProyectMVP.Application.StolenReports.GetByPlate;
using ProyectMVP.Application.StolenReports.Import;

namespace ProyectMVP.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SearchPlateHandler>();
        services.AddScoped<RegisterSightingHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<GetStolenReportsHandler>();
        services.AddScoped<ImportStolenReportsHandler>();
        services.AddScoped<GetHotspotsHandler>();
        return services;
    }
}
