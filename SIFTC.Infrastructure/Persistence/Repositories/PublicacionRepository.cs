using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Aprobaciones;
using SIFTC.Domain.Common;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class PublicacionRepository(ApplicationDbContext dbContext) : IPublicacionRepository
{
    public async Task<IReadOnlyList<PublicacionPendienteDto>> ListarPendientesAsync(
        CancellationToken cancellationToken = default)
    {
        return await (
            from publicacion in dbContext.PublicacionesLote.AsNoTracking()
            join lote in dbContext.LotesCarga.AsNoTracking()
                on publicacion.IdLote equals lote.IdLote
            where publicacion.Estado == "pendiente"
            orderby lote.FechaCarga descending
            select new PublicacionPendienteDto(
                publicacion.IdLote,
                lote.NombreArchivo,
                lote.FechaCarga,
                lote.RegistrosTotales,
                lote.RegistrosValidos,
                lote.EstadoCarga,
                publicacion.Estado,
                publicacion.IdUsuarioEjecutor,
                publicacion.IdUsuarioDecision,
                publicacion.FechaDecision,
                publicacion.Comentario))
            .ToListAsync(cancellationToken);
    }

    public Task<Result> AprobarAsync(
        Guid idLote,
        Guid idUsuario,
        CancellationToken cancellationToken = default) =>
        EjecutarFuncionAsync(
            "SELECT analitica.publicar_lote(@id_lote, @id_usuario)",
            [
                ("id_lote", idLote),
                ("id_usuario", idUsuario)
            ],
            cancellationToken);

    public Task<Result> RechazarAsync(
        Guid idLote,
        Guid idUsuario,
        string comentario,
        CancellationToken cancellationToken = default) =>
        EjecutarFuncionAsync(
            "SELECT analitica.rechazar_lote(@id_lote, @id_usuario, @comentario)",
            [
                ("id_lote", idLote),
                ("id_usuario", idUsuario),
                ("comentario", comentario)
            ],
            cancellationToken);

    private async Task<Result> EjecutarFuncionAsync(
        string sql,
        IReadOnlyList<(string Nombre, object? Valor)> parametros,
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
            command.CommandText = sql;

            foreach (var (nombre, valor) in parametros)
            {
                var parametro = command.CreateParameter();
                parametro.ParameterName = nombre;
                parametro.Value = valor ?? DBNull.Value;
                command.Parameters.Add(parametro);
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
            return Result.Success();
        }
        catch (PostgresException exception)
        {
            return Result.Failure(Traducir(exception));
        }
        finally
        {
            if (abrirConexion)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static Error Traducir(PostgresException exception) =>
        exception.SqlState switch
        {
            "P0002" => new Error("aprobacion.not_found", "El lote no existe.", ErrorType.NotFound),
            "SI403" => new Error("aprobacion.forbidden", "No tiene permiso para decidir este lote.", ErrorType.Forbidden),
            "SI409" => new Error("aprobacion.conflict", "El lote ya fue decidido o no admite esta operación.", ErrorType.Conflict),
            "SI422" => new Error("aprobacion.invalid", "La solicitud de aprobación no es válida.", ErrorType.Validation),
            _ => new Error("aprobacion.error", "No se pudo completar la decisión del lote.", ErrorType.Failure)
        };
}
