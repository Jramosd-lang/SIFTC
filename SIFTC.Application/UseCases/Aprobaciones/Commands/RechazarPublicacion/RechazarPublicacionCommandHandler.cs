using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Aprobaciones.Commands.RechazarPublicacion;

public sealed class RechazarPublicacionCommandHandler(IPublicacionRepository publicacionRepository)
    : IRequestHandler<RechazarPublicacionCommand, Result>
{
    public Task<Result> Handle(
        RechazarPublicacionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.IdLote == Guid.Empty)
        {
            return Task.FromResult(Result.Failure(
                new Error("aprobacion.lote.required", "El lote es obligatorio.", ErrorType.Validation)));
        }

        if (string.IsNullOrWhiteSpace(request.Comentario))
        {
            return Task.FromResult(Result.Failure(
                new Error("aprobacion.comentario.required", "El rechazo exige un comentario.", ErrorType.Validation)));
        }

        if (request.Comentario.Trim().Length > 500)
        {
            return Task.FromResult(Result.Failure(
                new Error("aprobacion.comentario.maxlength", "El comentario no puede superar 500 caracteres.", ErrorType.Validation)));
        }

        return publicacionRepository.RechazarAsync(
            request.IdLote,
            request.IdUsuario,
            request.Comentario.Trim(),
            cancellationToken);
    }
}
