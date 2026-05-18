using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProyectMVP.Application.Common.Interfaces;
using ProyectMVP.Identity.Providers;
using ProyectMVP.Identity.Security;

namespace ProyectMVP.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IIdentityProvider, DevIdentityProvider>();
        services.AddSingleton<ITokenIssuer, JwtTokenService>();
        return services;
    }
}
