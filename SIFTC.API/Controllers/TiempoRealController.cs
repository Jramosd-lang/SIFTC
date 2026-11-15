using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SIFTC.Api.Common;
using SIFTC.Api.Realtime;
using SIFTC.Application.UseCases.TiempoReal.Commands.DifundirCotizaciones;
using SIFTC.Domain.Common;

namespace SIFTC.Api.Controllers;

[Authorize(Roles = RolesSistema.Administrador)]
[ApiController]
[Route("api/v1/tiempo-real")]
public sealed class TiempoRealController(IMediator mediator, IOptions<TiempoRealOptions> options) : BaseApiController
{
    [HttpPost("emisiones-prueba")]
    [EndpointSummary("Emite una actualización de prueba por el hub de cotizaciones")]
    [EndpointDescription("""
        Rol admitido: Administrador. Solo existe si TiempoReal:EmisionPruebaHabilitada es true;
        si no, responde 404.

        Lo usa la prueba de carga de HU-BE-04 (SIFTC.LoadTests): envía el estado actual del ticker
        a todos los clientes de /hubs/cotizaciones con motivo "prueba". No modifica datos ni la
        secuencia, así que un frontend que descarta secuencias repetidas lo ignora.
        """)]
    [ProducesResponseType<EmisionPruebaResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> EmitirPrueba(CancellationToken cancellationToken)
    {
        if (!options.Value.EmisionPruebaHabilitada)
        {
            return HandleFailure(Result.Failure(new Error(
                "tiempo_real.prueba.deshabilitada",
                "La emisión de prueba está deshabilitada.",
                ErrorType.NotFound)));
        }

        var result = await mediator.Send(new DifundirCotizacionesCommand(), cancellationToken);
        if (result.IsFailure)
        {
            return HandleFailure(result);
        }

        return Accepted(new EmisionPruebaResponse(
            result.Value.Secuencia,
            result.Value.GeneradoEn,
            result.Value.Entidades.Count));
    }
}

/// <summary>Resumen de la emisión de prueba.</summary>
/// <param name="Secuencia">Secuencia del estado enviado (la misma de la última publicación).</param>
/// <param name="GeneradoEn">Momento UTC en que el servidor armó el mensaje.</param>
/// <param name="Entidades">Cantidad de entidades incluidas en el mensaje.</param>
public sealed record EmisionPruebaResponse(long Secuencia, DateTimeOffset GeneradoEn, int Entidades);
