using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Commands.EliminarRegla;

public sealed class EliminarReglaCommandHandler(INotificacionRepository repositorio)
    : IRequestHandler<EliminarReglaCommand, Result>
{
    public async Task<Result> Handle(EliminarReglaCommand request, CancellationToken cancellationToken)
    {
        var regla = await repositorio.ObtenerReglaAsync(request.IdRegla, cancellationToken);

        // Regla de otro usuario: mismo 404 que si no existiera (no se filtra información).
        if (regla is null || regla.IdUsuario != request.IdUsuario)
        {
            return Result.Failure(new Error(
                "notificacion.regla.no.encontrada",
                "La regla no existe.",
                ErrorType.NotFound));
        }

        if (regla.Activa)
        {
            regla.Desactivar();
            await repositorio.ActualizarReglaAsync(regla, cancellationToken);
        }

        return Result.Success();
    }
}
