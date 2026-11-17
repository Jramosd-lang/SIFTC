using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Common;
using NotificacionReglaEntidad = SIFTC.Domain.Entities.NotificacionRegla;

namespace SIFTC.Application.UseCases.Notificaciones.Commands.CrearRegla;

public sealed class CrearReglaCommandHandler(INotificacionRepository repositorio)
    : IRequestHandler<CrearReglaCommand, Result<NotificacionReglaDto>>
{
    public async Task<Result<NotificacionReglaDto>> Handle(
        CrearReglaCommand request,
        CancellationToken cancellationToken)
    {
        var creada = NotificacionReglaEntidad.Crear(
            request.IdUsuario,
            request.Canal,
            request.IdEntidad,
            request.Umbral);

        if (creada.IsFailure)
        {
            return Result.Failure<NotificacionReglaDto>(creada.Error);
        }

        var regla = creada.Value;

        if (regla.IdEntidad is not null)
        {
            var nombre = await repositorio.ObtenerNombreEntidadAsync(regla.IdEntidad.Value, cancellationToken);
            if (nombre is null)
            {
                return Result.Failure<NotificacionReglaDto>(new Error(
                    "notificacion.entidad.no.existe",
                    "La entidad indicada no existe.",
                    ErrorType.Validation));
            }
        }

        // El índice único es (usuario, canal, entidad): repetirla daría 500.
        var existente = await repositorio.ObtenerReglaPorClaveAsync(
            regla.IdUsuario,
            regla.Canal,
            regla.IdEntidad,
            cancellationToken);

        if (existente is not null)
        {
            if (existente.Activa)
            {
                return Result.Failure<NotificacionReglaDto>(new Error(
                    "notificacion.regla.duplicada",
                    "Ya existe una regla con ese canal y entidad.",
                    ErrorType.Conflict));
            }

            // Estaba desactivada: se reutiliza la fila (el índice único no distingue activa).
            existente.Reactivar(regla.Umbral);
            await repositorio.ActualizarReglaAsync(existente, cancellationToken);

            return Result.Success(new NotificacionReglaDto(
                existente.IdRegla,
                existente.Canal,
                existente.IdEntidad,
                existente.Umbral,
                existente.Activa,
                existente.FechaCreacion));
        }

        await repositorio.CrearReglaAsync(regla, cancellationToken);

        return Result.Success(new NotificacionReglaDto(
            regla.IdRegla,
            regla.Canal,
            regla.IdEntidad,
            regla.Umbral,
            regla.Activa,
            regla.FechaCreacion));
    }
}
