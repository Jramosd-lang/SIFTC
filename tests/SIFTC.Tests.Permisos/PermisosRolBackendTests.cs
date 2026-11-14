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

// Parte 1: quÃ© puede y quÃ© no puede hacer rol_backend en la base (lo que usa la API en producciÃ³n).
// Parte 2: la API corriendo como rol_backend sigue cumpliendo la matriz de la watchlist.
[Collection("Base de datos")]
public sealed class PermisosRolBackendTests(SiftcDatabaseFixture db)
{
    private const string SinPrivilegio = "42501"; // insufficient_privilege

    // ---------- Parte 1: permisos en la base ----------

    [Fact]
    public async Task Rol_backend_puede_leer_la_vista_de_cotizacion_vigente()
    {
        var error = await EjecutarComoBackendAsync("SELECT COUNT(*) FROM analitica.v_entidades_cotizacion");

        Assert.Null(error);
    }

    [Fact]
    public async Task Rol_backend_puede_registrar_en_la_bitacora_de_auditoria()
    {
        var error = await EjecutarComoBackendAsync(
            "INSERT INTO seguridad.auditorias (accion, detalle) VALUES ('prueba', 'verificaciÃ³n de permisos')");

        Assert.Null(error);
    }

    [Theory]
    [InlineData("SELECT COUNT(*) FROM limpia.tbl_entidades")]
    [InlineData("SELECT COUNT(*) FROM cruda.lotes_carga")]
    public async Task Rol_backend_no_puede_leer_capas_ni_bitacora_que_no_le_corresponden(string sql)
    {
        var error = await EjecutarComoBackendAsync(sql);

        Assert.NotNull(error);
        Assert.Equal(SinPrivilegio, error!.SqlState);
    }

    [Theory]
    [InlineData("DELETE FROM seguridad.usuarios")]
    [InlineData("DELETE FROM analitica.fct_cotizaciones")]
    [InlineData("UPDATE seguridad.roles SET nombre = nombre")]
    public async Task Rol_backend_no_puede_modificar_datos_fuera_de_su_alcance(string sql)
    {
        var error = await EjecutarComoBackendAsync(sql);

        Assert.NotNull(error);
        Assert.Equal(SinPrivilegio, error!.SqlState);
    }

    // ---------- Parte 2: la API como rol_backend ----------

    [Fact]
    public async Task Como_rol_backend_la_watchlist_agrega_y_quita_para_el_usuario_movil()
    {
        using var client = CrearClienteConRol(db.ApiBackend, RolesSistema.UsuarioMovil);

        var agregar = await client.PostAsync($"/api/v1/watchlist/{DatosDePrueba.IdEntidad}", null);
        Assert.Equal(HttpStatusCode.NoContent, agregar.StatusCode);

        var listar = await client.GetAsync("/api/v1/watchlist");
        Assert.Equal(HttpStatusCode.OK, listar.StatusCode);

        var quitar = await client.DeleteAsync($"/api/v1/watchlist/{DatosDePrueba.IdEntidad}");
        Assert.Equal(HttpStatusCode.NoContent, quitar.StatusCode);
    }

    [Theory]
    [InlineData(RolesSistema.Gerente)]
    [InlineData(RolesSistema.Auditor)]
    public async Task Como_rol_backend_la_watchlist_sigue_bloqueando_a_los_roles_sin_permiso(string rol)
    {
        using var client = CrearClienteConRol(db.ApiBackend, rol);

        var response = await client.PostAsync($"/api/v1/watchlist/{DatosDePrueba.IdEntidad}", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Auxiliares ----------

    private async Task<PostgresException?> EjecutarComoBackendAsync(string sql)
    {
        await using var conexion = new NpgsqlConnection(db.ConnectionStringBackend);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);

        try
        {
            await comando.ExecuteNonQueryAsync();
            return null;
        }
        catch (PostgresException ex)
        {
            return ex;
        }
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
