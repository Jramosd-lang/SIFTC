using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Queries.ListarReglas;

public sealed class ListarReglasQueryHandler(INotificacionRepository repositorio)
    : IRequestHandler<ListarReglasQuery, Result<IReadOnlyList<NotificacionReglaDto>>>
{
    public async Task<Result<IReadOnlyList<NotificacionReglaDto>>> Handle(
        ListarReglasQuery request,
        CancellationToken cancellationToken)
    {
        var reglas = await repositorio.ListarReglasAsync(request.IdUsuario, cancellationToken);

        IReadOnlyList<NotificacionReglaDto> dtos = reglas
            .Select(r => new NotificacionReglaDto(
                r.IdRegla,
                r.Canal,
                r.IdEntidad,
                r.Umbral,
                r.Activa,
                r.FechaCreacion))
            .ToList();

        return Result.Success(dtos);
    }
}
