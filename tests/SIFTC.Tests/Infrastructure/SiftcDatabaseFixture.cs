using System.Security.Cryptography;
using Npgsql;
using SIFTC.Tests;
using Testcontainers.PostgreSql;
using Xunit;

namespace SIFTC.Tests.Infrastructure;

// Levanta PostgreSQL 15, aplica los scripts en orden, define contraseñas aleatorias para los roles
// (no quedan en el repositorio), inserta datos de prueba y expone dos APIs:
// una como postgres (lógica) y otra como rol_backend (permisos reales de producción).
public sealed class SiftcDatabaseFixture : IAsyncLifetime
{
    private static readonly string[] ScriptsEnOrden =
    [
        "Script-1.sql",
        "Script-2.sql",
        "Script-3.sql",
        "Script-4.sql",
        "Script-5.sql",
        "Script-6.sql",
        "Script-7.sql",
        "Script-8.sql",
        "Script-9.sql",
        "Script-10.sql",
        "Script-11.sql",
        "Script-12.sql",
        "Script-13.sql",
        "Script-14.sql",
        "Script-15.sql",
        "Script-16.sql",
        "Script-17.sql",
        "Script-18-ajuste-diccionario.sql",
        "Script-19-flujo-aprobacion.sql",
        "Script-20-auditoria-lectura.sql",
        "Script-21-matriz-cruda-lectura.sql"
    ];

    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder()
        .WithImage("postgres:15")
        .WithDatabase("siftc_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly string _passwordRolBackend = GenerarPassword();

    private SiftcApiFactory? _apiPostgres;
    private SiftcApiFactory? _apiBackend;

    public string ConnectionString => _contenedor.GetConnectionString();

    public string ConnectionStringBackend
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
            {
                Username = "rol_backend",
                Password = _passwordRolBackend
            };
            return builder.ConnectionString;
        }
    }

    public SiftcApiFactory ApiPostgres => _apiPostgres ?? throw new InvalidOperationException("Fixture no inicializada.");

    public SiftcApiFactory ApiBackend => _apiBackend ?? throw new InvalidOperationException("Fixture no inicializada.");

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();
        await AplicarScriptsAsync();
        await DefinirPasswordsAsync();
        await DatosDePrueba.SembrarAsync(ConnectionString);
        _apiPostgres = new SiftcApiFactory(ConnectionString);
        _apiBackend = new SiftcApiFactory(ConnectionStringBackend);
    }

    public async Task DisposeAsync()
    {
        _apiPostgres?.Dispose();
        _apiBackend?.Dispose();
        await _contenedor.DisposeAsync();
    }

    private async Task AplicarScriptsAsync()
    {
        var carpeta = Path.Combine(AppContext.BaseDirectory, "Database", "Scripts");

        await using var conexion = new NpgsqlConnection(ConnectionString);
        await conexion.OpenAsync();

        foreach (var nombre in ScriptsEnOrden)
        {
            var sql = await File.ReadAllTextAsync(Path.Combine(carpeta, nombre));

            try
            {
                await using var comando = new NpgsqlCommand(sql, conexion);
                await comando.ExecuteNonQueryAsync();
            }
            catch (PostgresException ex)
            {
                throw new InvalidOperationException($"Falló {nombre}: {ex.MessageText}", ex);
            }
        }
    }

    // Las contraseñas se generan en cada ejecución y solo viven en memoria de la prueba.
    private async Task DefinirPasswordsAsync()
    {
        await using var conexion = new NpgsqlConnection(ConnectionString);
        await conexion.OpenAsync();

        foreach (var rol in new[] { "rol_etl", "rol_backend", "rol_powerbi" })
        {
            var password = rol == "rol_backend" ? _passwordRolBackend : GenerarPassword();
            await using var comando = new NpgsqlCommand($"ALTER ROLE {rol} PASSWORD '{password}'", conexion);
            await comando.ExecuteNonQueryAsync();
        }
    }

    private static string GenerarPassword() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
}

[CollectionDefinition("Base de datos")]
public sealed class BaseDeDatosCollection : ICollectionFixture<SiftcDatabaseFixture>
{
}
