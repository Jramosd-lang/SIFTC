using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Api.Common;
using SIFTC.Application.UseCases.Entidades;
using SIFTC.Application.UseCases.Watchlist.Commands.AgregarWatchlist;
using SIFTC.Application.UseCases.Watchlist.Commands.QuitarWatchlist;
using SIFTC.Application.UseCases.Watchlist.Queries.ListarWatchlist;
using SIFTC.Domain.Common;

namespace SIFTC.Api.Controllers;

[Authorize(Roles = RolesSistema.Consulta)]
[ApiController]
[Route("api/v1/watchlist")]
public sealed class WatchlistController(IMediator mediator) : BaseApiController
{
    [HttpGet]
    [EndpointSummary("Lista las entidades seguidas por el usuario autenticado")]
    [EndpointDescription("Devuelve la watchlist personal con la cotización actual de cada entidad. El usuario se toma del claim sub del JWT.")]
    [ProducesResponseType<IReadOnlyList<EntidadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
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
            new ListarWatchlistQuery(idUsuario.Value),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("{idEntidad:guid}")]
    [EndpointSummary("Agrega una entidad a la watchlist")]
    [EndpointDescription("Agrega la entidad a la watchlist del usuario autenticado. Es idempotente: si ya está seguida, responde 204.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Agregar(Guid idEntidad, CancellationToken cancellationToken)
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
            new AgregarWatchlistCommand(idUsuario.Value, idEntidad),
            cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpDelete("{idEntidad:guid}")]
    [EndpointSummary("Quita una entidad de la watchlist")]
    [EndpointDescription("Quita la entidad de la watchlist del usuario autenticado. Es idempotente: si no estaba seguida, responde 204.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> Quitar(Guid idEntidad, CancellationToken cancellationToken)
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
            new QuitarWatchlistCommand(idUsuario.Value, idEntidad),
            cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
