using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Aprobaciones;
using SIFTC.Application.UseCases.Aprobaciones.Commands.AprobarPublicacion;
using SIFTC.Application.UseCases.Aprobaciones.Commands.RechazarPublicacion;
using SIFTC.Application.UseCases.Aprobaciones.Queries.ListarPendientes;
using SIFTC.Domain.Common;

namespace SIFTC.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/aprobaciones")]
public sealed class AprobacionesController(IMediator mediator) : BaseApiController
{
    [Authorize(Roles = RolesSistema.AprobacionesLectura)]
    [HttpGet]
    [EndpointSummary("Lista los lotes pendientes de aprobación")]
    [EndpointDescription("""
        Roles admitidos para consultar: Administrador, Data Steward, Analista financiero y Auditor.
        Gerente / Directivo y Usuario móvil no tienen acceso.

        Devuelve los lotes en estado pendiente con sus datos de carga.
        Un lote solo puede ser aprobado o rechazado por un Analista financiero.
        """)]
    [ProducesResponseType<IReadOnlyList<PublicacionPendienteDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListarPendientesQuery(), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = RolesSistema.AprobacionesDecision)]
    [HttpPost("{idLote:guid}/aprobar")]
    [EndpointSummary("Aprueba un lote de cálculo")]
    [EndpointDescription("""
        Rol admitido: Analista financiero.

        Aprueba un lote pendiente y publica sus resultados en las tablas analíticas.
        El aprobador se obtiene del claim sub del JWT; no se recibe idUsuario por parámetro.
        Un analista no puede aprobar un lote que él mismo ejecutó.
        """)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> Aprobar(Guid idLote, CancellationToken cancellationToken)
    {
        var idUsuario = ObtenerUsuarioId();
        if (idUsuario is null)
        {
            return HandleFailure(Result.Failure(new Error(
                "auth.unauthorized",
                "No se pudo identificar el usuario autenticado.",
                ErrorType.Unauthorized)));
        }

        var result = await mediator.Send(
            new AprobarPublicacionCommand(idLote, idUsuario.Value),
            cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [Authorize(Roles = RolesSistema.AprobacionesDecision)]
    [HttpPost("{idLote:guid}/rechazar")]
    [EndpointSummary("Rechaza un lote de cálculo")]
    [EndpointDescription("""
        Rol admitido: Analista financiero.

        Rechaza un lote pendiente. El comentario es obligatorio y se registra en la
        notificación para el equipo de Análisis de datos.
        El usuario se obtiene del claim sub del JWT.
        """)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> Rechazar(
        Guid idLote,
        [FromBody] RechazarPublicacionRequest request,
        CancellationToken cancellationToken)
    {
        var idUsuario = ObtenerUsuarioId();
        if (idUsuario is null)
        {
            return HandleFailure(Result.Failure(new Error(
                "auth.unauthorized",
                "No se pudo identificar el usuario autenticado.",
                ErrorType.Unauthorized)));
        }

        var result = await mediator.Send(
            new RechazarPublicacionCommand(idLote, idUsuario.Value, request.Comentario),
            cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}

public sealed record RechazarPublicacionRequest(string Comentario);
