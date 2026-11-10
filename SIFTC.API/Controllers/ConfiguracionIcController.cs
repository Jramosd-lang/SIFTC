using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.ConfiguracionIc;
using SIFTC.Application.UseCases.ConfiguracionIc.Commands.CrearVersionIc;
using SIFTC.Application.UseCases.ConfiguracionIc.Queries.ListarVersionesIc;
using SIFTC.Application.UseCases.ConfiguracionIc.Queries.ObtenerConfiguracionIc;
using SIFTC.Domain.Common;

namespace SIFTC.Api.Controllers;

[Authorize(Roles = RolesSistema.Administrador)]
[ApiController]
[Route("api/v1/ic/configuracion")]
public sealed class ConfiguracionIcController(IMediator mediator) : BaseApiController
{
    [HttpGet]
    [EndpointSummary("Obtiene la configuracion vigente del IC")]
    [EndpointDescription("""
        Rol admitido: Administrador.

        Devuelve la version vigente con:
        - pesos por dimension en proporcion (por ejemplo 0.35),
        - umbrales por metrica,
        - frecuencia del IC: Diaria, Mensual o Trimestral.
        """)]
    [ProducesResponseType<ConfiguracionIcDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ObtenerConfiguracionIcQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("versiones")]
    [EndpointSummary("Lista las versiones de configuracion del IC")]
    [EndpointDescription("""
        Rol admitido: Administrador.

        Devuelve el historial de versiones, de la mas reciente a la mas antigua.
        Cada version incluye motivo, fecha de vigencia, usuario, pesos, umbrales y frecuencia.
        """)]
    [ProducesResponseType<IReadOnlyList<ConfiguracionIcDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> ListarVersiones(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListarVersionesIcQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("versiones")]
    [EndpointSummary("Crea una nueva version de configuracion del IC")]
    [EndpointDescription("""
        Rol admitido: Administrador.

        Cuerpo JSON:
        - motivo: obligatorio, maximo 500 caracteres.
        - frecuencia: Diaria, Mensual o Trimestral.
        - pesos: arreglo de { dimension, peso }. Los pesos usan proporcion (0.35) y deben sumar 1.000000 con tolerancia 0.000001.
        - umbrales: arreglo de { codigoMetrica, limiteInferior, limiteSuperior, severidad }.
          Severidad permitida: Informativa, Advertencia, Critica.

        La fecha de vigencia la asigna el servidor y la version aplica a calculos posteriores.
        """)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> Crear(
        [FromBody] CrearVersionIcRequest request,
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
            new CrearVersionIcCommand(
                idUsuario.Value,
                request.Motivo,
                request.Frecuencia,
                request.Pesos,
                request.Umbrales ?? []),
            cancellationToken);

        if (result.IsFailure)
        {
            return HandleFailure(result);
        }

        return Created(
            $"/api/v1/ic/configuracion/versiones/{result.Value}",
            new { idVersion = result.Value });
    }
}

public sealed record CrearVersionIcRequest(
    string Motivo,
    string Frecuencia,
    IReadOnlyList<PesoIcDto> Pesos,
    IReadOnlyList<UmbralIcDto>? Umbrales);
