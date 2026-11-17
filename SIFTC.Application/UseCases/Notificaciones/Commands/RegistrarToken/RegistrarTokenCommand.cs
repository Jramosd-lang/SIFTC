using MediatR;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Commands.RegistrarToken;

/// <summary>Registra el token FCM que entrega el frontend/móvil.</summary>
public sealed record RegistrarTokenCommand(
    Guid IdUsuario,
    string? Plataforma,
    string? Token) : IRequest<Result<NotificacionTokenDto>>;
