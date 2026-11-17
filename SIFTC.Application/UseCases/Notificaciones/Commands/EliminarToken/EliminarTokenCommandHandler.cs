using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Commands.EliminarToken;

public sealed class EliminarTokenCommandHandler(INotificacionRepository repositorio)
    : IRequestHandler<EliminarTokenCommand, Result>
{
    public async Task<Result> Handle(EliminarTokenCommand request, CancellationToken cancellationToken)
    {
        var eliminado = await repositorio.EliminarTokenAsync(
            request.IdUsuario,
            request.Token,
            cancellationToken);

        return eliminado
            ? Result.Success()
            : Result.Failure(new Error(
                "notificacion.token.no.encontrado",
                "El token no existe.",
                ErrorType.NotFound));
    }
}
