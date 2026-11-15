using SIFTC.Application.UseCases.TiempoReal;

namespace SIFTC.Application.Common.Interfaces.Realtime;

// Puerto de salida del tiempo real: la capa API lo implementa con SignalR.
public interface ICotizacionesNotificador
{
    Task DifundirAsync(
        ActualizacionCotizacionesDto actualizacion,
        CancellationToken cancellationToken = default);
}
