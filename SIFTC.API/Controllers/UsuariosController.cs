using MediatR;
using Microsoft.AspNetCore.Mvc;
using SIFTC.Application.UseCases.Usuarios.Commands;

namespace SIFTC.Api.Controllers;

[Route("api/[controller]")]
[ApiController] // Importante para que el [FromBody] se infiera automáticamente
public class UsuariosController(IMediator mediator) : ControllerBase
{
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] CrearUsuarioCommand command)
    {
        // Enviamos el comando a MediatR, el cual buscará tu CrearUsuarioCommandHandler
        var result = await mediator.Send(command);

        // Evaluamos el Result que devuelve tu Handler
        if (result.IsFailure)
        {
            // Devuelve un 400 Bad Request con el error del dominio (ej: "El correo ya está registrado")
            return BadRequest(new { error = result.Error });
        }

        // Si todo sale bien, devuelve un 200 OK con el Guid del nuevo usuario
        return Ok(new { idUsuario = result.Value });
    }
}