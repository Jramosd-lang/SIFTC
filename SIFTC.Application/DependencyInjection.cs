using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIFTC.Application.UseCases.TiempoReal;

namespace SIFTC.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<EstadoCotizacionesLector>();

        return services;
    }
}