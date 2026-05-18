using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProyectMVP.Application.Common.Interfaces;
using ProyectMVP.Infrastructure.Import;
using ProyectMVP.Infrastructure.Messaging;
using ProyectMVP.Infrastructure.Persistence;
using ProyectMVP.Infrastructure.Security;

namespace ProyectMVP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no está configurada.");

        services.AddDbContext<VehicleTheftDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ISightingRepository, SqlSightingRepository>();
        services.AddScoped<IStolenVehicleReportRepository, SqlStolenVehicleReportRepository>();
        services.AddScoped<IStolenVehicleImportRepository, SqlStolenVehicleImportRepository>();
        services.AddScoped<IStolenVehicleSource, JsonStolenVehicleSource>();
        services.AddScoped<IStolenVehicleSource, CsvStolenVehicleSource>();
        services.AddScoped<ISightingRegistrationRepository, SqlSightingRegistrationRepository>();
        services.AddScoped<IDeviceAuthService, DeviceAuthService>();
        services.AddSingleton<IMessageBus, InMemoryMessageBus>();

        return services;
    }
}
