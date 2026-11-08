using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Aprobaciones.Commands.AprobarPublicacion;

public sealed class AprobarPublicacionCommandHandler(IPublicacionRepository publicacionRepository)
    : IRequestHandler<AprobarPublicacionCommand, Result>
{
    public Task<Result> Handle(
        AprobarPublicacionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.IdLote == Guid.Empty)
        {
            return Task.FromResult(Result.Failure(
                new Error("aprobacion.lote.required", "El lote es obligatorio.", ErrorType.Validation)));
        }

        return publicacionRepository.AprobarAsync(
            request.IdLote,
            request.IdUsuario,
            cancellationToken);
    }
}
