using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.TiempoReal.Queries.ObtenerEstadoCotizaciones;

namespace SIFTC.Api.Realtime;

// HU-BE-04: canal de cotizaciones en tiempo real. Solo el servidor envía; el cliente no invoca métodos.
[Authorize(Roles = RolesSistema.Consulta)]
public sealed class CotizacionesHub(IMediator mediator, ILogger<CotizacionesHub> logger) : Hub
{
    public const string Ruta = "/hubs/cotizaciones";

    // Nombre exacto del evento: el cliente .NET distingue mayúsculas, por eso se fija aquí y no se deriva de un método.
    public const string Evento = "cotizaciones";

    // CA 2: al conectarse o reconectarse el cliente recibe el último estado completo.
    public override async Task OnConnectedAsync()
    {
        var result = await mediator.Send(new ObtenerEstadoCotizacionesQuery(), Context.ConnectionAborted);

        if (result.IsSuccess)
        {
            await Clients.Caller.SendAsync(Evento, result.Value, Context.ConnectionAborted);
        }
        else
        {
            // Se mantiene la conexión: el cliente recibirá la próxima difusión.
            logger.LogWarning(
                "No se pudo enviar el estado inicial a {ConnectionId}: {Error}",
                Context.ConnectionId,
                result.Error.Code);
        }

        await base.OnConnectedAsync();
    }
}
