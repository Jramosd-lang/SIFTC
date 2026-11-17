using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Application.UseCases.Notificaciones.Commands.CrearRegla;
using SIFTC.Application.UseCases.Notificaciones.Commands.EliminarRegla;
using SIFTC.Application.UseCases.Notificaciones.Commands.EliminarToken;
using SIFTC.Application.UseCases.Notificaciones.Commands.RegistrarEvento;
using SIFTC.Application.UseCases.Notificaciones.Commands.RegistrarToken;
using SIFTC.Application.UseCases.Notificaciones.Queries.ListarEnvios;
using SIFTC.Application.UseCases.Notificaciones.Queries.ListarReglas;
using SIFTC.Domain.Common;

namespace SIFTC.Api.Controllers;

/// <summary>Cuerpo de POST /api/v1/notificaciones/reglas (CA2).</summary>
public sealed record CrearReglaRequest(string? Canal, Guid? IdEntidad, decimal? Umbral);

/// <summary>Cuerpo de POST /api/v1/notificaciones/tokens.</summary>
public sealed record RegistrarTokenRequest(string? Plataforma, string? Token);

/// <summary>Cuerpo de POST /api/v1/notificaciones/eventos (entrada del ETL).</summary>
public sealed record RegistrarEventoRequest(Guid IdEntidad, decimal VariacionPct, string? NombreEntidad);

// HU-AD-10 / HU-MO-05: reglas por usuario (CA2), tokens FCM y cola de envíos (CA1/CA3).
[Authorize(Roles = RolesSistema.Consulta)]
[ApiController]
[Route("api/v1/notificaciones")]
public sealed class NotificacionesController(IMediator mediator) : BaseApiController
{
    [HttpGet("reglas")]
    [EndpointSummary("Lista las reglas de notificación propias")]
    [EndpointDescription("""
        CA2. Devuelve las reglas activas del usuario autenticado: canal
        (correo, push o ambos), entidad (null = todas) y umbral (null = cualquier cambio).
        """)]
    [ProducesResponseType<IReadOnlyList<NotificacionReglaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> ListarReglas(CancellationToken cancellationToken)
    {
        var idUsuario = ObtenerUsuarioId();
        if (idUsuario is null)
        {
            return SinUsuario();
        }

        return HandleResult(await mediator.Send(new ListarReglasQuery(idUsuario.Value), cancellationToken));
    }

    [HttpPost("reglas")]
    [EndpointSummary("Crea una regla de notificación propia")]
    [EndpointDescription("""
        CA2. Cuerpo: {"canal":"correo|push|ambos","idEntidad":"uuid opcional","umbral":2.5}.

        - idEntidad ausente o null = todas las entidades.
        - umbral ausente o null = cualquier cambio; si viene, debe ser >= 0 y se
          interpreta como variación mínima en porcentaje (valor absoluto).
        - Una regla repetida (mismo canal y entidad) devuelve 409; si estaba
          desactivada, se reactiva con el umbral nuevo.
        """)]
    [ProducesResponseType<NotificacionReglaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> CrearRegla(
        [FromBody] CrearReglaRequest request,
        CancellationToken cancellationToken)
    {
        var idUsuario = ObtenerUsuarioId();
        if (idUsuario is null)
        {
            return SinUsuario();
        }

        return HandleResult(await mediator.Send(
            new CrearReglaCommand(idUsuario.Value, request.Canal, request.IdEntidad, request.Umbral),
            cancellationToken));
    }

    [HttpDelete("reglas/{idRegla:guid}")]
    [EndpointSummary("Desactiva una regla propia")]
    [EndpointDescription("CA2. Regla de otro usuario o inexistente: 404 (no se filtra información).")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> EliminarRegla(Guid idRegla, CancellationToken cancellationToken)
    {
        var idUsuario = ObtenerUsuarioId();
        if (idUsuario is null)
        {
            return SinUsuario();
        }

        return HandleResult(await mediator.Send(
            new EliminarReglaCommand(idUsuario.Value, idRegla),
            cancellationToken));
    }

    [HttpPost("tokens")]
    [EndpointSummary("Registra el token FCM del dispositivo")]
    [EndpointDescription("""
        Cuerpo: {"plataforma":"web|android|ios","token":"<token FCM>"}.

        El token es único: si ya existía se reasigna al usuario autenticado.
        """)]
    [ProducesResponseType<NotificacionTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> RegistrarToken(
        [FromBody] RegistrarTokenRequest request,
        CancellationToken cancellationToken)
    {
        var idUsuario = ObtenerUsuarioId();
        if (idUsuario is null)
        {
            return SinUsuario();
        }

        return HandleResult(await mediator.Send(
            new RegistrarTokenCommand(idUsuario.Value, request.Plataforma, request.Token),
            cancellationToken));
    }

    [HttpDelete("tokens/{token}")]
    [EndpointSummary("Da de baja un token FCM propio")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> EliminarToken(string token, CancellationToken cancellationToken)
    {
        var idUsuario = ObtenerUsuarioId();
        if (idUsuario is null)
        {
            return SinUsuario();
        }

        return HandleResult(await mediator.Send(
            new EliminarTokenCommand(idUsuario.Value, token),
            cancellationToken));
    }

    [HttpGet("envios")]
    [EndpointSummary("Consulta la cola y el log de envíos")]
    [EndpointDescription("""
        CA1/CA3. El Administrador ve todos los envíos; los demás roles solo los propios.

        Parámetros:
        - estado (opcional): pendiente, enviado o fallido.
        - limite (opcional): 1 a 200; por defecto 50.

        Cada fila trae intentos, maxIntentos, ultimoError, fechaCreacion y fechaEnvio:
        con fechaCreacion → fechaEnvio se verifica el envío en menos de 1 minuto.
        """)]
    [ProducesResponseType<IReadOnlyList<NotificacionEnvioDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> ListarEnvios(
        [FromQuery] string? estado,
        [FromQuery] int? limite,
        CancellationToken cancellationToken)
    {
        var idUsuario = ObtenerUsuarioId();
        if (idUsuario is null)
        {
            return SinUsuario();
        }

        return HandleResult(await mediator.Send(
            new ListarEnviosQuery(
                idUsuario.Value,
                User.IsInRole(RolesSistema.Administrador),
                estado,
                limite),
            cancellationToken));
    }

    [HttpPost("eventos")]
    [Authorize(Roles = RolesSistema.NotificacionesEvento)]
    [EndpointSummary("Registra un evento relevante y evalúa las reglas")]
    [EndpointDescription("""
        CA1/CA2. Punto de entrada para el ETL/backend: {"idEntidad":"uuid",
        "variacionPct":3.2,"nombreEntidad":"opcional"}.

        Evalúa las reglas activas de todos los usuarios y encola los envíos que
        correspondan (una regla 'ambos' genera dos filas). El worker los envía en
        la siguiente pasada (≤ 10 s por defecto). Responde {"encolados": n}.

        Roles admitidos: Administrador y Analista financiero.
        """)]
    [ProducesResponseType<RegistrarEventoResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> RegistrarEvento(
        [FromBody] RegistrarEventoRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(await mediator.Send(
            new RegistrarEventoCommand(request.IdEntidad, request.VariacionPct, request.NombreEntidad),
            cancellationToken));
    }

    private IActionResult SinUsuario() =>
        HandleFailure(Result.Failure(new Error(
            "auth.unauthorized",
            "Token inválido o vencido",
            ErrorType.Unauthorized)));
}
