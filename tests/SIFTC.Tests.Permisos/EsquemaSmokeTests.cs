using Npgsql;
using SIFTC.Tests.Infrastructure;
using Xunit;

namespace SIFTC.Tests;

// Verifica que los scripts se aplicaron de verdad sobre PostgreSQL real.
[Collection("Base de datos")]
public sealed class EsquemaSmokeTests(SiftcDatabaseFixture db)
{
    [Fact]
    public async Task Los_scripts_crean_la_vista_de_cotizacion_vigente()
    {
        var existe = await ScalarAsync(
            "SELECT COUNT(*) FROM pg_views WHERE schemaname = 'analitica' AND viewname = 'v_entidades_cotizacion'");

        Assert.Equal(1L, existe);
    }

    [Fact]
    public async Task Los_scripts_crean_los_tres_roles_de_aplicacion()
    {
        var existen = await ScalarAsync(
            "SELECT COUNT(*) FROM pg_roles WHERE rolname IN ('rol_etl', 'rol_backend', 'rol_powerbi')");

        Assert.Equal(3L, existen);
    }

    private async Task<long> ScalarAsync(string sql)
    {
        await using var conexion = new NpgsqlConnection(db.ConnectionString);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return (long)(await comando.ExecuteScalarAsync())!;
    }
}
