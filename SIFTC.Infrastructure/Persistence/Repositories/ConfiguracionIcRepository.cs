using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.ConfiguracionIc;
using SIFTC.Domain.Common;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class ConfiguracionIcRepository(ApplicationDbContext dbContext)
    : IConfiguracionIcRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ConfiguracionIcDto?> ObtenerActivaAsync(
        CancellationToken cancellationToken = default)
    {
        var versiones = await ListarAsync(
            """
            WHERE v.fecha_vigencia <= now()
            ORDER BY v.fecha_vigencia DESC, v.numero_version DESC
            LIMIT 1
            """,
            cancellationToken);

        return versiones.FirstOrDefault();
    }

    public Task<IReadOnlyList<ConfiguracionIcDto>> ListarVersionesAsync(
        CancellationToken cancellationToken = default) =>
        ListarAsync(
            "ORDER BY v.fecha_vigencia DESC, v.numero_version DESC LIMIT 100",
            cancellationToken);

    public async Task<Result<Guid>> CrearVersionAsync(
        Guid idUsuario,
        string motivo,
        string frecuencia,
        IReadOnlyList<PesoIcDto> pesos,
        IReadOnlyList<UmbralIcDto> umbrales,
        CancellationToken cancellationToken = default)
    {
        var pesosJson = JsonSerializer.Serialize(
            pesos.Select(p => new { dimension = p.Dimension, peso = p.Peso }));

        var umbralesJson = JsonSerializer.Serialize(
            umbrales.Select(u => new
            {
                codigo_metrica = u.CodigoMetrica,
                limite_inferior = u.LimiteInferior,
                limite_superior = u.LimiteSuperior,
                severidad = u.Severidad
            }));

        var connection = dbContext.Database.GetDbConnection();
        var abrirConexion = connection.State != ConnectionState.Open;

        if (abrirConexion)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT analitica.crear_version_ic(
                    @id_usuario,
                    @motivo,
                    @frecuencia,
                    CAST(@pesos AS jsonb),
                    CAST(@umbrales AS jsonb))
                """;

            command.Parameters.Add(CrearParametro("id_usuario", idUsuario));
            command.Parameters.Add(CrearParametro("motivo", motivo));
            command.Parameters.Add(CrearParametro("frecuencia", frecuencia));
            command.Parameters.Add(CrearParametro("pesos", pesosJson));
            command.Parameters.Add(CrearParametro("umbrales", umbralesJson));

            var value = await command.ExecuteScalarAsync(cancellationToken);
            return value is Guid idVersion
                ? Result.Success(idVersion)
                : Result.Failure<Guid>(new Error(
                    "configuracionIc.error",
                    "No se pudo crear la version del IC.",
                    ErrorType.Failure));
        }
        catch (PostgresException exception)
        {
            return Result.Failure<Guid>(Traducir(exception));
        }
        finally
        {
            if (abrirConexion)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<IReadOnlyList<ConfiguracionIcDto>> ListarAsync(
        string filtro,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var abrirConexion = connection.State != ConnectionState.Open;

        if (abrirConexion)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT
                    v.id_version,
                    v.numero_version,
                    v.fecha_vigencia,
                    v.motivo,
                    v.id_usuario,
                    v.frecuencia_ic,
                    COALESCE((
                        SELECT json_agg(json_build_object(
                            'dimension', d.nombre,
                            'peso', p.peso) ORDER BY d.nombre)
                        FROM analitica.configuracion_ic_peso p
                        JOIN analitica.dim_dimension d ON d.id_dimension = p.id_dimension
                        WHERE p.id_version = v.id_version
                    ), '[]'::json)::text AS pesos,
                    COALESCE((
                        SELECT json_agg(json_build_object(
                            'codigoMetrica', m.codigo,
                            'limiteInferior', u.limite_inferior,
                            'limiteSuperior', u.limite_superior,
                            'severidad', u.severidad) ORDER BY m.codigo)
                        FROM analitica.configuracion_ic_umbral u
                        JOIN analitica.dim_metrica m ON m.id_metrica = u.id_metrica
                        WHERE u.id_version = v.id_version
                    ), '[]'::json)::text AS umbrales
                FROM analitica.configuracion_ic_version v
                {filtro}
                """;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var versiones = new List<ConfiguracionIcDto>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var pesos = JsonSerializer.Deserialize<List<PesoIcDto>>(
                    reader.GetString(6), JsonOptions) ?? [];

                var umbrales = JsonSerializer.Deserialize<List<UmbralIcDto>>(
                    reader.GetString(7), JsonOptions) ?? [];

                versiones.Add(new ConfiguracionIcDto(
                    reader.GetGuid(0),
                    reader.GetInt32(1),
                    reader.GetFieldValue<DateTimeOffset>(2),
                    reader.GetString(3),
                    reader.GetGuid(4),
                    reader.GetString(5),
                    pesos,
                    umbrales));
            }

            return versiones;
        }
        finally
        {
            if (abrirConexion)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static NpgsqlParameter CrearParametro(string nombre, object? valor)
    {
        var parametro = new NpgsqlParameter(nombre, valor ?? DBNull.Value);
        return parametro;
    }

    private static Error Traducir(PostgresException exception) =>
        exception.SqlState switch
        {
            "SI403" => new Error("configuracionIc.forbidden", "Solo el Administrador puede configurar el IC.", ErrorType.Forbidden),
            "SI422" => new Error("configuracionIc.invalid", "La configuracion del IC no es valida.", ErrorType.Validation),
            _ => new Error("configuracionIc.error", "No se pudo crear la version del IC.", ErrorType.Failure)
        };
}
