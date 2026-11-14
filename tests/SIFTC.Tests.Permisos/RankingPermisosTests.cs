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

// Ranking (D1 y acceso del Usuario móvil).
// Esta prueba verifica autorización. Que el Gerente vea posicion y cambioPosicion en null
// requiere datos reales del microservicio de Python, así que se prueba cuando esté disponible.
[Collection("Base de datos")]
public sealed class RankingPermisosTests(SiftcDatabaseFixture db)
{
    public static TheoryData<string, bool> PermisosRanking => new()
    {
        { RolesSistema.Administrador, true },
        { RolesSistema.DataSteward, true },
        { RolesSistema.AnalistaFinanciero, true },
        { RolesSistema.Gerente, true },
        { RolesSistema.Auditor, true },
        { RolesSistema.UsuarioMovil, false }
    };

    [Theory]
    [MemberData(nameof(PermisosRanking))]
    public async Task Ranking_respeta_la_matriz(string rol, bool puedeConsultar)
    {
        using var client = CrearClienteConRol(rol);

        var response = await client.GetAsync("/api/v1/ranking");

        if (puedeConsultar)
        {
            // La autorización pasa. Como el microservicio no está disponible en pruebas,
            // la respuesta no puede ser 401 ni 403.
            Assert.True(response.StatusCode is not HttpStatusCode.Unauthorized and not HttpStatusCode.Forbidden,
                $"Rol {rol}: la autorización debería pasar, recibido {(int)response.StatusCode}.");
        }
        else
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Ranking_sin_token_responde_401()
    {
        using var client = db.ApiPostgres.CreateClient();

        var response = await client.GetAsync("/api/v1/ranking");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
