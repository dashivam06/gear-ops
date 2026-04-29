using gearOps.Application.Interfaces;
using gearOps.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace gearOps.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
