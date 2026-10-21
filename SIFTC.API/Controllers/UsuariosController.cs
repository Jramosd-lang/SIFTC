using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Application.UseCases.Usuarios.Commands;

namespace SIFTC.Api.Controllers;

[Route("api/v1/[controller]")]
[ApiController] // Importante para que el [FromBody] se infiera automáticamente
public class UsuariosController(IMediator mediator) : BaseApiController
{
    [Authorize(Roles = "Administrador")]
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] CrearUsuarioCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
