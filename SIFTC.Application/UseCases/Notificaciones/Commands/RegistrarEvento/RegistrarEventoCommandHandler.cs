using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Commands.RegistrarEvento;

public sealed class RegistrarEventoCommandHandler(
    INotificacionRepository repositorio,
    GeneradorNotificaciones generador)
    : IRequestHandler<RegistrarEventoCommand, Result<RegistrarEventoResponseDto>>
{
    public async Task<Result<RegistrarEventoResponseDto>> Handle(
        RegistrarEventoCommand request,
        CancellationToken cancellationToken)
    {
        if (request.IdEntidad == Guid.Empty)
        {
            return Result.Failure<RegistrarEventoResponseDto>(new Error(
                "notificacion.evento.entidad.required",
                "La entidad del evento es obligatoria.",
                ErrorType.Validation));
        }

        var nombre = await repositorio.ObtenerNombreEntidadAsync(request.IdEntidad, cancellationToken);
        if (nombre is null)
        {
            return Result.Failure<RegistrarEventoResponseDto>(new Error(
                "notificacion.entidad.no.existe",
                "La entidad indicada no existe.",
                ErrorType.Validation));
        }

        var evento = new EventoNotificacionDto(
            request.IdEntidad,
            string.IsNullOrWhiteSpace(request.NombreEntidad) ? nombre : request.NombreEntidad.Trim(),
            request.VariacionPct);

        var encolados = await generador.EncolarPorEventoAsync([evento], cancellationToken);

        return Result.Success(new RegistrarEventoResponseDto(encolados));
    }
}
