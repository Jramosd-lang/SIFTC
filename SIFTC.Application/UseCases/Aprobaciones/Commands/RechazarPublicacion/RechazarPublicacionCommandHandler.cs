using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Aprobaciones.Commands.RechazarPublicacion;

public sealed class RechazarPublicacionCommandHandler(
    IPublicacionRepository publicacionRepository,
    IAuditoriaRepository auditoriaRepository)
    : IRequestHandler<RechazarPublicacionCommand, Result>
{
    public async Task<Result> Handle(
        RechazarPublicacionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.IdLote == Guid.Empty)
        {
            return Result.Failure(
                new Error("aprobacion.lote.required", "El lote es obligatorio.", ErrorType.Validation));
        }

        if (string.IsNullOrWhiteSpace(request.Comentario))
        {
            return Result.Failure(
                new Error("aprobacion.comentario.required", "El rechazo exige un comentario.", ErrorType.Validation));
        }

        if (request.Comentario.Trim().Length > 500)
        {
            return Result.Failure(
                new Error("aprobacion.comentario.maxlength", "El comentario no puede superar 500 caracteres.", ErrorType.Validation));
        }

        var resultado = await publicacionRepository.RechazarAsync(
            request.IdLote,
            request.IdUsuario,
            request.Comentario.Trim(),
            cancellationToken);

        if (resultado.IsSuccess)
        {
            await auditoriaRepository.RegistrarAsync(
                "publicacion.rechazada",
                $"Lote {request.IdLote}",
                request.IdUsuario,
                cancellationToken);
        }

        return resultado;
    }
}
