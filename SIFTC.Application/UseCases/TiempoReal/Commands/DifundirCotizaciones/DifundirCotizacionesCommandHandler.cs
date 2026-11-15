using MediatR;
using SIFTC.Application.Common.Interfaces.Realtime;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.TiempoReal.Commands.DifundirCotizaciones;

public sealed class DifundirCotizacionesCommandHandler(
    EstadoCotizacionesLector lector,
    ICotizacionesNotificador notificador)
    : IRequestHandler<DifundirCotizacionesCommand, Result<ActualizacionCotizacionesDto>>
{
    public async Task<Result<ActualizacionCotizacionesDto>> Handle(
        DifundirCotizacionesCommand request,
        CancellationToken cancellationToken)
    {
        var actualizacion = await lector.LeerAsync(MotivoActualizacion.Prueba, cancellationToken);
        await notificador.DifundirAsync(actualizacion, cancellationToken);
        return Result.Success(actualizacion);
    }
}
