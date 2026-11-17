using Microsoft.Extensions.Options;
using SIFTC.Application.UseCases.Notificaciones;

namespace SIFTC.Api.Notifications;

/// <summary>
/// CA1: worker en background que revisa la cola cada pocos segundos y envía los
/// pendientes por correo o push. El intervalo se configura con
/// Notificaciones__IntervaloSegundos (por defecto 10 s).
/// </summary>
public sealed class NotificacionesWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificacionesOptions> options,
    ILogger<NotificacionesWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = TimeSpan.FromSeconds(Math.Max(1, options.Value.IntervaloSegundos));
        logger.LogInformation(
            "Worker de notificaciones activo (intervalo {Intervalo} s).",
            intervalo.TotalSeconds);

        using var temporizador = new PeriodicTimer(intervalo);

        try
        {
            while (await temporizador.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var alcance = scopeFactory.CreateAsyncScope();
                    var procesador = alcance.ServiceProvider
                        .GetRequiredService<ProcesadorEnviosNotificacion>();

                    var procesados = await procesador.ProcesarPendientesAsync(stoppingToken);

                    if (procesados > 0)
                    {
                        logger.LogInformation(
                            "Worker de notificaciones procesó {Procesados} envíos.",
                            procesados);
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Error en una pasada del worker de notificaciones.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado normal de la API.
        }
    }
}
