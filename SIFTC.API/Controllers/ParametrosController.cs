using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SIFTC.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ParametrosController : BaseApiController
{
    [Authorize(Roles = "Analista financiero,Administrador")]
    [HttpPost("actualizar-umbral")]
    public IActionResult ActualizarUmbral() => Ok("Umbral actualizado");
}
