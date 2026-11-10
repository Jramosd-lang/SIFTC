using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SIFTC.Api.Common;
using SIFTC.Tests.Infrastructure;
using Xunit;

namespace SIFTC.Tests;

// Pruebas contra PostgreSQL real: los permisos se verifican con la API y los cambios, en la base.
[Collection("Base de datos")]
public sealed class WatchlistPermisosTests(SiftcDatabaseFixture db)
{
    public static TheoryData<string> TodosLosRoles => new()
    {
        RolesSistema.Administrador,
        RolesSistema.DataSteward,
        RolesSistema.AnalistaFinanciero,
        RolesSistema.Gerente,
        RolesSistema.Auditor,
        RolesSistema.UsuarioMovil
    };

    [Theory]
    [MemberData(nameof(TodosLosRoles))]
    public async Task Listar_permite_leer_la_watchlist_a_todos_los_roles(string rol)
    {
        using var client = CrearClienteConRol(rol);

        var response = await client.GetAsync("/api/v1/watchlist");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TodosLosRoles))]
    public async Task Agregar_solo_lo_permite_al_usuario_movil(string rol)
    {
        using var client = CrearClienteConRol(rol);
        var idUsuario = DatosDePrueba.UsuarioPorRol[rol];

        var response = await client.PostAsync($"/api/v1/watchlist/{DatosDePrueba.IdEntidad}", null);

        var esperado = rol == RolesSistema.UsuarioMovil ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden;
        var cuerpo = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == esperado,
            $"Rol {rol}: esperado {(int)esperado}, recibido {(int)response.StatusCode}. Cuerpo: {cuerpo}");

        if (rol != RolesSistema.UsuarioMovil)
        {
            Assert.Equal(0L, await ContarSeguidasAsync(idUsuario));
        }
    }

    [Theory]
    [MemberData(nameof(TodosLosRoles))]
    public async Task Quitar_solo_lo_permite_al_usuario_movil(string rol)
    {
        using var client = CrearClienteConRol(rol);

        var response = await client.DeleteAsync($"/api/v1/watchlist/{DatosDePrueba.IdEntidad}");

        var esperado = rol == RolesSistema.UsuarioMovil ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden;
        var cuerpo = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == esperado,
            $"Rol {rol}: esperado {(int)esperado}, recibido {(int)response.StatusCode}. Cuerpo: {cuerpo}");
    }

    [Fact]
    public async Task Agregar_y_quitar_se_reflejan_en_la_base_de_datos()
    {
        using var client = CrearClienteConRol(RolesSistema.UsuarioMovil);
        var idUsuario = DatosDePrueba.UsuarioPorRol[RolesSistema.UsuarioMovil];

        var agregar = await client.PostAsync($"/api/v1/watchlist/{DatosDePrueba.IdEntidad}", null);
        Assert.Equal(HttpStatusCode.NoContent, agregar.StatusCode);
        Assert.Equal(1L, await ContarSeguidasAsync(idUsuario));

        var quitar = await client.DeleteAsync($"/api/v1/watchlist/{DatosDePrueba.IdEntidad}");
        Assert.Equal(HttpStatusCode.NoContent, quitar.StatusCode);
        Assert.Equal(0L, await ContarSeguidasAsync(idUsuario));
    }

    [Fact]
    public async Task Listar_sin_token_responde_401()
    {
        using var client = db.ApiPostgres.CreateClient();

        var response = await client.GetAsync("/api/v1/watchlist");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CrearClienteConRol(string rol)
    {
        var client = db.ApiPostgres.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerarToken(rol));
        return client;
    }

    private async Task<long> ContarSeguidasAsync(Guid idUsuario)
    {
        await using var conexion = new NpgsqlConnection(db.ConnectionString);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "SELECT COUNT(*) FROM seguridad.entidades_seguidas WHERE id_usuario = @usuario", conexion);
        comando.Parameters.AddWithValue("usuario", idUsuario);
        return (long)(await comando.ExecuteScalarAsync())!;
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
