using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SIFTC.Api.Common;
using SIFTC.Tests.Infrastructure;
using Xunit;

namespace SIFTC.Tests;

// CA 2: las respuestas de los endpoints de consulta no contienen campos Confidenciales
// (datos financieros detallados ni posición competitiva) para los roles que no los tienen en la matriz.
[Collection("Base de datos")]
public sealed class SensibilidadCamposTests(SiftcDatabaseFixture db)
{
    // Campos Confidenciales según el diccionario de datos.
    private static readonly string[] CamposConfidenciales =
    [
        "utilidad",
        "patrimonio",
        "activos",
        "pasivo",
        "ebitda",
        "ingresos",
        "inventarios",
        "gastos",
        "posicion"
    ];

    public static TheoryData<string, string> ConsultasSinCamposConfidenciales => new()
    {
        { RolesSistema.UsuarioMovil, "/api/v1/entidades" },
        { RolesSistema.UsuarioMovil, "/api/v1/watchlist" },
        { RolesSistema.UsuarioMovil, "/api/v1/metricas" },
        { RolesSistema.Gerente, "/api/v1/entidades" },
        { RolesSistema.Gerente, "/api/v1/metricas" }
    };

    [Theory]
    [MemberData(nameof(ConsultasSinCamposConfidenciales))]
    public async Task La_respuesta_no_incluye_campos_confidenciales(string rol, string ruta)
    {
        using var client = CrearClienteConRol(rol);

        var response = await client.GetAsync(ruta);
        var cuerpo = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Rol {rol} en {ruta}: esperado 200, recibido {(int)response.StatusCode}. Cuerpo: {cuerpo}");

        foreach (var campo in CamposConfidenciales)
        {
            Assert.DoesNotContain(campo, cuerpo, StringComparison.OrdinalIgnoreCase);
        }
    }

    private HttpClient CrearClienteConRol(string rol)
    {
        var client = db.ApiPostgres.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerarToken(rol));
        return client;
    }

    private static string GenerarToken(string rol)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SiftcApiFactory.Secret));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, DatosDePrueba.UsuarioPorRol[rol].ToString()),
            new Claim(ClaimTypes.Role, rol)
        };

        var token = new JwtSecurityToken(
            issuer: SiftcApiFactory.Issuer,
            audience: SiftcApiFactory.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
