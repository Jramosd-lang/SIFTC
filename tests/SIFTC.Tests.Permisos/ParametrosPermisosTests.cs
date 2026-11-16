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

// D3: lectura de parámetros para Administrador, Analista financiero y Auditor. El resto, 403.
[Collection("Base de datos")]
public sealed class ParametrosPermisosTests(SiftcDatabaseFixture db)
{
    public static TheoryData<string, HttpStatusCode> PermisosLectura => new()
    {
        { RolesSistema.Administrador, HttpStatusCode.OK },
        { RolesSistema.AnalistaFinanciero, HttpStatusCode.OK },
        { RolesSistema.Auditor, HttpStatusCode.OK },
        { RolesSistema.Gerente, HttpStatusCode.Forbidden },
        { RolesSistema.DataSteward, HttpStatusCode.Forbidden },
        { RolesSistema.UsuarioMovil, HttpStatusCode.Forbidden }
    };

    [Theory]
    [MemberData(nameof(PermisosLectura))]
    public async Task Leer_parametros_respeta_la_matriz(string rol, HttpStatusCode esperado)
    {
        using var client = CrearClienteConRol(rol);

        var response = await client.GetAsync("/api/v1/parametros");
        var cuerpo = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == esperado,
            $"Rol {rol}: esperado {(int)esperado}, recibido {(int)response.StatusCode}. Cuerpo: {cuerpo}");
    }

    [Fact]
    public async Task Auditor_recibe_pesos_del_ic_y_metricas_con_formula_y_frecuencia()
    {
        using var client = CrearClienteConRol(RolesSistema.Auditor);

        var response = await client.GetAsync("/api/v1/parametros");
        var cuerpo = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"pesoIc\"", cuerpo);
        Assert.Contains("\"formula\"", cuerpo);
        Assert.Contains("\"frecuencia\"", cuerpo);
        Assert.Contains("RNT-01", cuerpo);
    }

    [Fact]
    public async Task Auditor_no_puede_modificar_parametros()
    {
        using var client = CrearClienteConRol(RolesSistema.Auditor);

        var response = await client.PostAsync("/api/v1/parametros/actualizar-umbral", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
