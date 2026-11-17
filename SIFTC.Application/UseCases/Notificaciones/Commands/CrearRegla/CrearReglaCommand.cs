using MediatR;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Commands.CrearRegla;

/// <summary>CA2: crea una regla propia (canal, entidad opcional y umbral opcional).</summary>
public sealed record CrearReglaCommand(
    Guid IdUsuario,
    string? Canal,
    Guid? IdEntidad,
    decimal? Umbral) : IRequest<Result<NotificacionReglaDto>>;
