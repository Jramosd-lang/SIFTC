using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ParametrosController : ControllerBase
{
    // Este endpoint solo podrá ser tocado por alguien con rol "Analista" o "Administrador"
    // Si alguien con rol "Consulta" (o sin token) intenta entrar, C# lanzará el error 403 automáticamente
    [Authorize(Roles = "Analista, Administrador")]
    [HttpPost("actualizar-umbral")]
    public IActionResult ActualizarUmbral()
    {
        return Ok("Umbral actualizado");
    }
}