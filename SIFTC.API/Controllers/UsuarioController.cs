using MediatR;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Application.UseCases.Auth.Commands.AutenticarUsuario;

namespace SIFTC.Api.Controllers;

[Route("api/[controller]")]
public class AuthController(IMediator mediator) : BaseApiController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AutenticarUsuarioCommand command)
    {
        var result = await mediator.Send(command);
        return HandleResult(result);
    }
}