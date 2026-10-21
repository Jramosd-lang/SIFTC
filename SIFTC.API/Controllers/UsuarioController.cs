using MediatR;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Application.UseCases.Auth.Commands.AutenticarUsuario;

namespace SIFTC.Api.Controllers;

[Route("api/v1/[controller]")]
public sealed class AuthController(IMediator mediator) : BaseApiController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AutenticarUsuarioCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
