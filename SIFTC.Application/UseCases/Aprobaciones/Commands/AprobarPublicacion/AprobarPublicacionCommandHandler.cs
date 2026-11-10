using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Aprobaciones.Commands.AprobarPublicacion;

public sealed class AprobarPublicacionCommandHandler(
    IPublicacionRepository publicacionRepository,
    IAuditoriaRepository auditoriaRepository)
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

        var resultado = await publicacionRepository.AprobarAsync(
            request.IdLote,
            request.IdUsuario,
            cancellationToken);

        if (resultado.IsSuccess)
        {
            await auditoriaRepository.RegistrarAsync(
                "publicacion.aprobada",
                $"Lote {request.IdLote}",
                request.IdUsuario,
                cancellationToken);
        }

        return resultado;
    }
}
