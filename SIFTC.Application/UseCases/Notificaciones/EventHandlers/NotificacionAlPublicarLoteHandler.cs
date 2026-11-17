using MediatR;
using Microsoft.Extensions.Logging;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Aprobaciones.Events;
using SIFTC.Application.UseCases.Notificaciones;

namespace SIFTC.Application.UseCases.Notificaciones.EventHandlers;

/// <summary>
/// CA1: al publicar un lote se encolan de inmediato las notificaciones de los
/// usuarios cuyas reglas cubren las entidades del lote y su variación.
/// </summary>
public sealed class NotificacionAlPublicarLoteHandler(
    INotificacionRepository repositorio,
    GeneradorNotificaciones generador,
    ILogger<NotificacionAlPublicarLoteHandler> logger)
    : INotificationHandler<LotePublicadoEvent>
{
    public async Task Handle(LotePublicadoEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var eventos = await repositorio.ObtenerVariacionesLoteAsync(
                notification.IdLote,
                cancellationToken);

            var encolados = await generador.EncolarPorEventoAsync(eventos, cancellationToken);

            if (encolados > 0)
            {
                logger.LogInformation(
                    "Lote {IdLote} publicado: {Encolados} notificaciones encoladas.",
                    notification.IdLote,
                    encolados);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // La aprobación ya quedó confirmada: un fallo al notificar no la revierte.
            logger.LogError(
                exception,
                "No se pudieron encolar las notificaciones del lote {IdLote}.",
                notification.IdLote);
        }
    }
}
