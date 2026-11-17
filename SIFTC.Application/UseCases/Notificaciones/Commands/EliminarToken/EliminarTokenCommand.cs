using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Commands.EliminarToken;

/// <summary>Da de baja un token FCM propio.</summary>
public sealed record EliminarTokenCommand(Guid IdUsuario, string Token) : IRequest<Result>;
