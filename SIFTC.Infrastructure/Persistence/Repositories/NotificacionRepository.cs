using System.Data;
using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Notificaciones;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class NotificacionRepository(ApplicationDbContext dbContext) : INotificacionRepository
{
    // ------------------------------------------------------------------
    // CA2: reglas
    // ------------------------------------------------------------------

    public async Task<IReadOnlyList<NotificacionRegla>> ListarReglasAsync(
        Guid idUsuario,
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificacionesReglas
            .AsNoTracking()
            .Where(r => r.IdUsuario == idUsuario && r.Activa)
            .OrderByDescending(r => r.FechaCreacion)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NotificacionRegla>> ListarReglasActivasAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificacionesReglas
            .AsNoTracking()
            .Where(r => r.Activa)
            .ToListAsync(cancellationToken);

    public async Task<NotificacionRegla?> ObtenerReglaAsync(
        Guid idRegla,
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificacionesReglas
            .FirstOrDefaultAsync(r => r.IdRegla == idRegla, cancellationToken);

    public async Task<NotificacionRegla?> ObtenerReglaPorClaveAsync(
        Guid idUsuario,
        string canal,
        Guid? idEntidad,
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificacionesReglas
            .FirstOrDefaultAsync(
                r => r.IdUsuario == idUsuario && r.Canal == canal && r.IdEntidad == idEntidad,
                cancellationToken);

    public async Task CrearReglaAsync(
        NotificacionRegla regla,
        CancellationToken cancellationToken = default)
    {
        await dbContext.NotificacionesReglas.AddAsync(regla, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarReglaAsync(
        NotificacionRegla regla,
        CancellationToken cancellationToken = default)
    {
        dbContext.NotificacionesReglas.Update(regla);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> ObtenerNombreEntidadAsync(
        Guid idEntidad,
        CancellationToken cancellationToken = default) =>
        // rol_backend no tiene acceso a limpia.tbl_entidades: el nombre sale de la
        // vista analítica que ya usan los endpoints de entidades.
        await dbContext.EntidadesCotizacion
            .AsNoTracking()
            .Where(e => e.IdEntidad == idEntidad)
            .Select(e => e.Nombre)
            .FirstOrDefaultAsync(cancellationToken);

    // ------------------------------------------------------------------
    // Tokens FCM
    // ------------------------------------------------------------------

    public async Task RegistrarTokenAsync(
        NotificacionToken token,
        CancellationToken cancellationToken = default)
    {
        var existente = await dbContext.NotificacionesTokens
            .FirstOrDefaultAsync(t => t.TokenFcm == token.TokenFcm, cancellationToken);

        if (existente is null)
        {
            await dbContext.NotificacionesTokens.AddAsync(token, cancellationToken);
        }
        else
        {
            existente.Reasignar(token.IdUsuario, token.Plataforma);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EliminarTokenAsync(
        Guid idUsuario,
        string tokenFcm,
        CancellationToken cancellationToken = default)
    {
        var token = await dbContext.NotificacionesTokens
            .FirstOrDefaultAsync(
                t => t.TokenFcm == tokenFcm && t.IdUsuario == idUsuario,
                cancellationToken);

        if (token is null)
        {
            return false;
        }

        dbContext.NotificacionesTokens.Remove(token);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<NotificacionToken>> ObtenerTokensActivosAsync(
        Guid idUsuario,
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificacionesTokens
            .AsNoTracking()
            .Where(t => t.IdUsuario == idUsuario && t.Activo)
            .ToListAsync(cancellationToken);

    public async Task DesactivarTokenAsync(
        string tokenFcm,
        CancellationToken cancellationToken = default)
    {
        var token = await dbContext.NotificacionesTokens
            .FirstOrDefaultAsync(t => t.TokenFcm == tokenFcm, cancellationToken);

        if (token is null || !token.Activo)
        {
            return;
        }

        token.Desactivar();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarcarTokenUsoAsync(
        Guid idToken,
        CancellationToken cancellationToken = default)
    {
        var token = await dbContext.NotificacionesTokens
            .FirstOrDefaultAsync(t => t.IdToken == idToken, cancellationToken);

        if (token is null)
        {
            return;
        }

        token.MarcarUso();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // ------------------------------------------------------------------
    // CA1 / CA3: cola de envíos
    // ------------------------------------------------------------------

    public async Task<string?> ObtenerCorreoAsync(
        Guid idUsuario,
        CancellationToken cancellationToken = default) =>
        await dbContext.Usuarios
            .AsNoTracking()
            .Where(u => u.IdUsuario == idUsuario)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task EncolarAsync(
        IReadOnlyList<NotificacionEnvio> envios,
        CancellationToken cancellationToken = default)
    {
        if (envios.Count == 0)
        {
            return;
        }

        await dbContext.NotificacionesEnvios.AddRangeAsync(envios, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificacionEnvio>> TomarPendientesAsync(
        int limite,
        DateTimeOffset reintentoVencidoAntesDe,
        CancellationToken cancellationToken = default) =>
        await dbContext.NotificacionesEnvios
            .Where(e => e.Estado == "pendiente" &&
                        (e.FechaUltimoIntento == null ||
                         e.FechaUltimoIntento <= reintentoVencidoAntesDe))
            .OrderBy(e => e.FechaCreacion)
            .Take(limite)
            .ToListAsync(cancellationToken);

    public async Task ActualizarEnvioAsync(
        NotificacionEnvio envio,
        CancellationToken cancellationToken = default)
    {
        dbContext.NotificacionesEnvios.Update(envio);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificacionEnvioDto>> ListarEnviosAsync(
        Guid? idUsuario,
        string? estado,
        int limite,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.NotificacionesEnvios.AsNoTracking();

        if (idUsuario is not null)
        {
            query = query.Where(e => e.IdUsuario == idUsuario);
        }

        if (estado is not null)
        {
            query = query.Where(e => e.Estado == estado);
        }

        return await query
            .OrderByDescending(e => e.FechaCreacion)
            .Take(limite)
            .Select(e => new NotificacionEnvioDto(
                e.IdEnvio,
                e.IdRegla,
                e.Canal,
                e.Asunto,
                e.Estado,
                e.Intentos,
                e.MaxIntentos,
                e.UltimoError,
                e.FechaCreacion,
                e.FechaUltimoIntento,
                e.FechaEnvio))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EventoNotificacionDto>> ObtenerVariacionesLoteAsync(
        Guid idLote,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH actual AS (
                SELECT i.id_entidad, i.ic
                FROM analitica.fct_indices_competitividad i
                WHERE i.id_lote = @idLote
            ),
            anterior AS (
                SELECT DISTINCT ON (i.id_entidad) i.id_entidad, i.ic
                FROM analitica.fct_indices_competitividad i
                JOIN actual a ON a.id_entidad = i.id_entidad
                WHERE i.id_lote <> @idLote
                ORDER BY i.id_entidad, i.fecha_carga DESC, i.id_indice DESC
            )
            SELECT a.id_entidad,
                   e.nombre,
                   CASE WHEN p.ic IS NULL OR p.ic = 0 THEN 0::numeric
                        ELSE round(((a.ic - p.ic) / p.ic) * 100, 4)
                   END AS variacion_pct
            FROM actual a
            JOIN analitica.v_entidades_cotizacion e ON e.id_entidad = a.id_entidad
            LEFT JOIN anterior p ON p.id_entidad = a.id_entidad
            """;

        var connection = dbContext.Database.GetDbConnection();
        var abrirConexion = connection.State != ConnectionState.Open;

        if (abrirConexion)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var comando = connection.CreateCommand();
            comando.CommandText = sql;

            var parametro = comando.CreateParameter();
            parametro.ParameterName = "idLote";
            parametro.Value = idLote;
            comando.Parameters.Add(parametro);

            await using var lector = await comando.ExecuteReaderAsync(cancellationToken);

            var eventos = new List<EventoNotificacionDto>();
            while (await lector.ReadAsync(cancellationToken))
            {
                eventos.Add(new EventoNotificacionDto(
                    lector.GetGuid(0),
                    lector.GetString(1),
                    lector.GetDecimal(2)));
            }

            return eventos;
        }
        finally
        {
            if (abrirConexion)
            {
                await connection.CloseAsync();
            }
        }
    }
}
