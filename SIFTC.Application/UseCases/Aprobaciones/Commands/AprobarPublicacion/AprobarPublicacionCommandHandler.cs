using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Aprobaciones.Events;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Aprobaciones.Commands.AprobarPublicacion;

public sealed class AprobarPublicacionCommandHandler(
    IPublicacionRepository publicacionRepository,
    IPublisher publisher)
    : IRequestHandler<AprobarPublicacionCommand, Result>
{
    public async Task<Result> Handle(
        AprobarPublicacionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.IdLote == Guid.Empty)
        {
            return Result.Failure(
                new Error("aprobacion.lote.required", "El lote es obligatorio.", ErrorType.Validation));
        }

        var result = await publicacionRepository.AprobarAsync(
            request.IdLote,
            request.IdUsuario,
            cancellationToken);

        if (result.IsSuccess)
        {
            await publisher.Publish(new LotePublicadoEvent(request.IdLote, request.IdUsuario), cancellationToken);
        }

        return result;
    }
}
