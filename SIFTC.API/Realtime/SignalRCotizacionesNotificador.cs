using Microsoft.AspNetCore.SignalR;
using SIFTC.Application.Common.Interfaces.Realtime;
using SIFTC.Application.UseCases.TiempoReal;

namespace SIFTC.Api.Realtime;

// Adaptador del puerto ICotizacionesNotificador: envía la actualización a todos los clientes del hub.
public sealed class SignalRCotizacionesNotificador(IHubContext<CotizacionesHub> hubContext)
    : ICotizacionesNotificador
{
    public Task DifundirAsync(
        ActualizacionCotizacionesDto actualizacion,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.All.SendAsync(CotizacionesHub.Evento, actualizacion, cancellationToken);
}
