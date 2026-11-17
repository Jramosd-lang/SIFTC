using MediatR;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Queries.ListarReglas;

/// <summary>CA2: reglas propias del usuario autenticado.</summary>
public sealed record ListarReglasQuery(Guid IdUsuario)
    : IRequest<Result<IReadOnlyList<NotificacionReglaDto>>>;
