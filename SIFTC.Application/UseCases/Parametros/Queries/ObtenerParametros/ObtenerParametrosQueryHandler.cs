using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Parametros.Queries.ObtenerParametros;

public sealed class ObtenerParametrosQueryHandler(IParametrosRepository parametrosRepository)
    : IRequestHandler<ObtenerParametrosQuery, Result<ParametrosDto>>
{
    public async Task<Result<ParametrosDto>> Handle(
        ObtenerParametrosQuery request,
        CancellationToken cancellationToken)
    {
        var parametros = await parametrosRepository.ObtenerAsync(cancellationToken);

        return Result.Success(parametros);
    }
}
