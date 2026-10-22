using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Entidades.Queries.ObtenerEntidad;

public class ObtenerEntidadQueryHandler(IEntidadRepository entidadRepository)
    : IRequestHandler<ObtenerEntidadQuery, Result<EntidadDto>>
{
    public async Task<Result<EntidadDto>> Handle(ObtenerEntidadQuery request, CancellationToken cancellationToken)
    {
        var entidad = await entidadRepository.GetByIdAsync(request.IdEntidad, cancellationToken);

        if (entidad is null)
        {
            return Result.Failure<EntidadDto>(new Error("entidad.not_found", "La entidad solicitada no existe.", ErrorType.NotFound));
        }

        return Result.Success(EntidadDto.FromEntity(entidad));
    }
}
