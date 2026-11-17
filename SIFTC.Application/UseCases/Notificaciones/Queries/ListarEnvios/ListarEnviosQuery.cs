using MediatR;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Queries.ListarEnvios;

/// <summary>CA3: consulta de la cola/log. El administrador ve todos; el resto, los propios.</summary>
public sealed record ListarEnviosQuery(
    Guid IdUsuario,
    bool EsAdministrador,
    string? Estado,
    int? Limite) : IRequest<Result<IReadOnlyList<NotificacionEnvioDto>>>;
