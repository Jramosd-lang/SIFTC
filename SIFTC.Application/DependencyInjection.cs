using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIFTC.Application.UseCases.Notificaciones;
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

        // HU-AD-10 / HU-MO-05: generador de avisos y procesador de la cola.
        services.AddScoped<GeneradorNotificaciones>();
        services.AddScoped<ProcesadorEnviosNotificacion>();

        return services;
    }
}