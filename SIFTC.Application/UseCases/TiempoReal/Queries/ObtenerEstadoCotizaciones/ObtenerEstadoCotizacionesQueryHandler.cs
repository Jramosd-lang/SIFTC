using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.TiempoReal.Queries.ObtenerEstadoCotizaciones;

public sealed class ObtenerEstadoCotizacionesQueryHandler(EstadoCotizacionesLector lector)
    : IRequestHandler<ObtenerEstadoCotizacionesQuery, Result<ActualizacionCotizacionesDto>>
{
    public async Task<Result<ActualizacionCotizacionesDto>> Handle(
        ObtenerEstadoCotizacionesQuery request,
        CancellationToken cancellationToken)
    {
        var estado = await lector.LeerAsync(MotivoActualizacion.EstadoInicial, cancellationToken);
        return Result.Success(estado);
    }
}
