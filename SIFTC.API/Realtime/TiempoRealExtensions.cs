using SIFTC.Application.Common.Interfaces.Realtime;

namespace SIFTC.Api.Realtime;

public static class TiempoRealExtensions
{
    public static IServiceCollection AddTiempoReal(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TiempoRealOptions>(configuration.GetSection(TiempoRealOptions.SectionName));

        // Una sola instancia en Render: los clientes viven en memoria y no hace falta backplane (Redis).
        services.AddSignalR(options =>
        {
            // Mantiene viva la conexión detrás del proxy de Render y detecta clientes caídos.
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            // El cliente no envía datos: se limita el tamaño de lo que puede mandar.
            options.MaximumReceiveMessageSize = 4 * 1024;
        });

        services.AddSingleton<ICotizacionesNotificador, SignalRCotizacionesNotificador>();

        return services;
    }

    public static IEndpointRouteBuilder MapTiempoReal(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<CotizacionesHub>(CotizacionesHub.Ruta);
        return endpoints;
    }
}
