using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.ConfiguracionIc.Queries.ListarVersionesIc;

public sealed class ListarVersionesIcQueryHandler(
    IConfiguracionIcRepository configuracionIcRepository)
    : IRequestHandler<ListarVersionesIcQuery, Result<IReadOnlyList<ConfiguracionIcDto>>>
{
    public async Task<Result<IReadOnlyList<ConfiguracionIcDto>>> Handle(
        ListarVersionesIcQuery request,
        CancellationToken cancellationToken)
    {
        var versiones = await configuracionIcRepository.ListarVersionesAsync(cancellationToken);
        return Result.Success(versiones);
    }
}
