using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SIFTC.Api.Common;
using SIFTC.Tests.Infrastructure;
using Xunit;

namespace SIFTC.Tests;

// HU-BE-05: lectura de la bandeja y decisión (aprobar / rechazar) según la matriz.
// Estas pruebas verifican autorización. El flujo completo de un lote (carga, aprobación real)
// se prueba aparte cuando haya lotes pendientes de datos de prueba.
[Collection("Base de datos")]
public sealed class AprobacionesPermisosTests(SiftcDatabaseFixture db)
{
    private static readonly Guid IdLoteInexistente = Guid.Parse("00000000-0000-0000-0000-0000000000f1");

    public static TheoryData<string, bool> PermisosLectura => new()
    {
        { RolesSistema.Administrador, true },
        { RolesSistema.DataSteward, true },
        { RolesSistema.AnalistaFinanciero, true },
        { RolesSistema.Auditor, true },
        { RolesSistema.Gerente, false },
        { RolesSistema.UsuarioMovil, false }
    };

    public static TheoryData<string, bool> PermisosDecision => new()
    {
        { RolesSistema.AnalistaFinanciero, true },
        { RolesSistema.Administrador, false },
        { RolesSistema.DataSteward, false },
        { RolesSistema.Auditor, false },
        { RolesSistema.Gerente, false },
        { RolesSistema.UsuarioMovil, false }
    };

    [Theory]
    [MemberData(nameof(PermisosLectura))]
    public async Task Bandeja_de_aprobaciones_respeta_la_matriz(string rol, bool puedeLeer)
    {
        using var client = CrearClienteConRol(rol);

        var response = await client.GetAsync("/api/v1/aprobaciones");
        var cuerpo = await response.Content.ReadAsStringAsync();

        var esperado = puedeLeer ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
        Assert.True(response.StatusCode == esperado,
            $"Rol {rol}: esperado {(int)esperado}, recibido {(int)response.StatusCode}. Cuerpo: {cuerpo}");
    }

    [Theory]
    [MemberData(nameof(PermisosDecision))]
    public async Task Aprobar_solo_lo_permite_al_analista(string rol, bool puedeDecidir)
    {
        using var client = CrearClienteConRol(rol);

        var response = await client.PostAsync($"/api/v1/aprobaciones/{IdLoteInexistente}/aprobar", null);

        AssertDecision(rol, puedeDecidir, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(PermisosDecision))]
    public async Task Rechazar_solo_lo_permite_al_analista(string rol, bool puedeDecidir)
    {
        using var client = CrearClienteConRol(rol);

        var response = await client.PostAsync(
            $"/api/v1/aprobaciones/{IdLoteInexistente}/rechazar",
            JsonContent.Create(new { comentario = "Rechazo de prueba" }));

        AssertDecision(rol, puedeDecidir, response.StatusCode);
    }

    [Fact]
    public async Task Bandeja_sin_token_responde_401()
    {
        using var client = db.ApiPostgres.CreateClient();

        var response = await client.GetAsync("/api/v1/aprobaciones");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Si no puede decidir debe ser 403. Si puede, la autorización pasa: el lote no existe,
    // así que la respuesta no puede ser 401 ni 403.
    private static void AssertDecision(string rol, bool puedeDecidir, HttpStatusCode recibido)
    {
        if (puedeDecidir)
        {
            Assert.True(recibido is not HttpStatusCode.Unauthorized and not HttpStatusCode.Forbidden,
                $"Rol {rol}: la autorización debería pasar, recibido {(int)recibido}.");
        }
        else
        {
            Assert.True(recibido == HttpStatusCode.Forbidden,
                $"Rol {rol}: esperado 403, recibido {(int)recibido}.");
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
