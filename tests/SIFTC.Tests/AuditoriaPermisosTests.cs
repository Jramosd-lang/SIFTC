using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SIFTC.Api.Common;
using SIFTC.Tests.Infrastructure;
using Xunit;

namespace SIFTC.Tests;

// D4: la bitácora se consulta solo para Administrador y Auditor, y los eventos críticos se registran.
[Collection("Base de datos")]
public sealed class AuditoriaPermisosTests(SiftcDatabaseFixture db)
{
    public static TheoryData<string, bool> PermisosLectura => new()
    {
        { RolesSistema.Administrador, true },
        { RolesSistema.Auditor, true },
        { RolesSistema.DataSteward, false },
        { RolesSistema.AnalistaFinanciero, false },
        { RolesSistema.Gerente, false },
        { RolesSistema.UsuarioMovil, false }
    };

    [Theory]
    [MemberData(nameof(PermisosLectura))]
    public async Task Bitacora_respeta_la_matriz(string rol, bool puedeLeer)
    {
        using var client = CrearClienteConRol(db.ApiPostgres, rol);

        var response = await client.GetAsync("/api/v1/auditoria");
        var cuerpo = await response.Content.ReadAsStringAsync();

        var esperado = puedeLeer ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
        Assert.True(response.StatusCode == esperado,
            $"Rol {rol}: esperado {(int)esperado}, recibido {(int)response.StatusCode}. Cuerpo: {cuerpo}");
    }

    [Fact]
    public async Task Bitacora_sin_token_responde_401()
    {
        using var client = db.ApiPostgres.CreateClient();

        var response = await client.GetAsync("/api/v1/auditoria");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rol_backend_puede_consultar_la_bitacora_como_administrador()
    {
        using var client = CrearClienteConRol(db.ApiBackend, RolesSistema.Administrador);

        var response = await client.GetAsync("/api/v1/auditoria");
        var cuerpo = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Recibido {(int)response.StatusCode}. Cuerpo: {cuerpo}");
    }

    [Fact]
    public async Task Login_fallido_queda_registrado_en_la_bitacora()
    {
        using var client = db.ApiPostgres.CreateClient();
        var usuarioInexistente = $"noexiste-{Guid.NewGuid():N}@prueba.local";

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { usuario = usuarioInexistente, password = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var registros = await ContarEventosAsync("login.fallido");
        Assert.True(registros >= 1, "No se registró el evento login.fallido.");
    }

    private async Task<long> ContarEventosAsync(string accion)
    {
        await using var conexion = new NpgsqlConnection(db.ConnectionString);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT COUNT(*) FROM seguridad.auditorias WHERE accion = @accion", conexion);
        comando.Parameters.AddWithValue("accion", accion);
        return (long)(await comando.ExecuteScalarAsync())!;
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
