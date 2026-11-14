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

// Matriz financiera cruda (Confidencial): solo Data Steward y Auditor la leen.
[Collection("Base de datos")]
public sealed class MatrizCrudaPermisosTests(SiftcDatabaseFixture db)
{
    public static TheoryData<string, bool> PermisosLectura => new()
    {
        { RolesSistema.DataSteward, true },
        { RolesSistema.Auditor, true },
        { RolesSistema.Administrador, false },
        { RolesSistema.AnalistaFinanciero, false },
        { RolesSistema.Gerente, false },
        { RolesSistema.UsuarioMovil, false }
    };

    [Theory]
    [MemberData(nameof(PermisosLectura))]
    public async Task Matriz_cruda_respeta_la_matriz(string rol, bool puedeLeer)
    {
        using var client = CrearClienteConRol(db.ApiPostgres, rol);

        var response = await client.GetAsync("/api/v1/matriz-cruda");
        var cuerpo = await response.Content.ReadAsStringAsync();

        var esperado = puedeLeer ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
        Assert.True(response.StatusCode == esperado,
            $"Rol {rol}: esperado {(int)esperado}, recibido {(int)response.StatusCode}. Cuerpo: {cuerpo}");
    }

    [Fact]
    public async Task Matriz_cruda_sin_token_responde_401()
    {
        using var client = db.ApiPostgres.CreateClient();

        var response = await client.GetAsync("/api/v1/matriz-cruda");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rol_backend_puede_leer_la_matriz_cruda_como_data_steward()
    {
        using var client = CrearClienteConRol(db.ApiBackend, RolesSistema.DataSteward);

        var response = await client.GetAsync("/api/v1/matriz-cruda");
        var cuerpo = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Recibido {(int)response.StatusCode}. Cuerpo: {cuerpo}");
    }

    private static HttpClient CrearClienteConRol(SiftcApiFactory api, string rol)
    {
        var client = api.CreateClient();
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
