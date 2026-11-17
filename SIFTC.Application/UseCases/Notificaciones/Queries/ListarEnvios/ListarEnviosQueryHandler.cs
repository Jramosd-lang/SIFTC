using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Queries.ListarEnvios;

public sealed class ListarEnviosQueryHandler(INotificacionRepository repositorio)
    : IRequestHandler<ListarEnviosQuery, Result<IReadOnlyList<NotificacionEnvioDto>>>
{
    private static readonly string[] EstadosValidos = ["pendiente", "enviado", "fallido"];

    public async Task<Result<IReadOnlyList<NotificacionEnvioDto>>> Handle(
        ListarEnviosQuery request,
        CancellationToken cancellationToken)
    {
        var estado = string.IsNullOrWhiteSpace(request.Estado)
            ? null
            : request.Estado.Trim().ToLowerInvariant();

        if (estado is not null && !EstadosValidos.Contains(estado, StringComparer.Ordinal))
        {
            return Result.Failure<IReadOnlyList<NotificacionEnvioDto>>(new Error(
                "notificacion.envio.estado.invalid",
                "El estado admite: pendiente, enviado o fallido.",
                ErrorType.Validation));
        }

        var limite = request.Limite ?? 50;
        if (limite is < 1 or > 200)
        {
            return Result.Failure<IReadOnlyList<NotificacionEnvioDto>>(new Error(
                "notificacion.envio.limite.invalid",
                "El límite debe estar entre 1 y 200.",
                ErrorType.Validation));
        }

        var envios = await repositorio.ListarEnviosAsync(
            request.EsAdministrador ? null : request.IdUsuario,
            estado,
            limite,
            cancellationToken);

        return Result.Success(envios);
    }
}
