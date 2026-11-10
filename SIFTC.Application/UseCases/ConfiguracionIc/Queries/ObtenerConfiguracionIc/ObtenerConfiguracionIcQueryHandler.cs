using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.ConfiguracionIc.Queries.ObtenerConfiguracionIc;

public sealed class ObtenerConfiguracionIcQueryHandler(
    IConfiguracionIcRepository configuracionIcRepository)
    : IRequestHandler<ObtenerConfiguracionIcQuery, Result<ConfiguracionIcDto>>
{
    public async Task<Result<ConfiguracionIcDto>> Handle(
        ObtenerConfiguracionIcQuery request,
        CancellationToken cancellationToken)
    {
        var configuracion = await configuracionIcRepository.ObtenerActivaAsync(cancellationToken);

        return configuracion is null
            ? Result.Failure<ConfiguracionIcDto>(new Error(
                "configuracionIc.not_found",
                "No hay una configuracion del IC registrada.",
                ErrorType.NotFound))
            : Result.Success(configuracion);
    }
}
