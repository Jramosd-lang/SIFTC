using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Aprobaciones.Queries.ListarPendientes;

public sealed class ListarPendientesQueryHandler(IPublicacionRepository publicacionRepository)
    : IRequestHandler<ListarPendientesQuery, Result<IReadOnlyList<PublicacionPendienteDto>>>
{
    public async Task<Result<IReadOnlyList<PublicacionPendienteDto>>> Handle(
        ListarPendientesQuery request,
        CancellationToken cancellationToken)
    {
        var pendientes = await publicacionRepository.ListarPendientesAsync(cancellationToken);
        return Result.Success(pendientes);
    }
}
