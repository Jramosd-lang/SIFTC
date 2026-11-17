using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Common;
using NotificacionTokenEntidad = SIFTC.Domain.Entities.NotificacionToken;

namespace SIFTC.Application.UseCases.Notificaciones.Commands.RegistrarToken;

public sealed class RegistrarTokenCommandHandler(INotificacionRepository repositorio)
    : IRequestHandler<RegistrarTokenCommand, Result<NotificacionTokenDto>>
{
    public async Task<Result<NotificacionTokenDto>> Handle(
        RegistrarTokenCommand request,
        CancellationToken cancellationToken)
    {
        var creado = NotificacionTokenEntidad.Crear(
            request.IdUsuario,
            request.Plataforma,
            request.Token);

        if (creado.IsFailure)
        {
            return Result.Failure<NotificacionTokenDto>(creado.Error);
        }

        await repositorio.RegistrarTokenAsync(creado.Value, cancellationToken);

        return Result.Success(new NotificacionTokenDto(
            creado.Value.IdToken,
            creado.Value.Plataforma,
            creado.Value.Activo,
            creado.Value.FechaRegistro));
    }
}
