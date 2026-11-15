using MediatR;
using Microsoft.Extensions.Logging;
using SIFTC.Application.Common.Interfaces.Realtime;
using SIFTC.Application.UseCases.Aprobaciones.Events;

namespace SIFTC.Application.UseCases.TiempoReal.EventHandlers;

// CA 1: cada lote aprobado se difunde a los clientes conectados.
public sealed class DifundirAlPublicarLoteHandler(
    EstadoCotizacionesLector lector,
    ICotizacionesNotificador notificador,
    ILogger<DifundirAlPublicarLoteHandler> logger)
    : INotificationHandler<LotePublicadoEvent>
{
    public async Task Handle(LotePublicadoEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var actualizacion = await lector.LeerAsync(MotivoActualizacion.LotePublicado, cancellationToken);
            await notificador.DifundirAsync(actualizacion, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // La aprobación ya quedó confirmada en la base de datos: un fallo del tiempo real no la revierte.
            // Los clientes recuperan el estado al reconectarse.
            logger.LogError(exception, "No se pudo difundir el lote publicado {IdLote}.", notification.IdLote);
        }
    }
}
