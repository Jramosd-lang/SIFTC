using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class DiccionarioRepository(ApplicationDbContext dbContext) : IDiccionarioRepository
{
    public async Task<int> ReemplazarTodoAsync(
        IReadOnlyList<Diccionario> filas,
        CancellationToken cancellationToken = default)
    {
        // Un solo SaveChanges: EF emite los DELETE y los INSERT en la misma
        // transacción, así que la tabla nunca queda a medias.
        var existentes = await dbContext.Diccionario
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        dbContext.Diccionario.RemoveRange(existentes);
        await dbContext.Diccionario.AddRangeAsync(filas, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return filas.Count;
    }

    public async Task<IReadOnlyList<Diccionario>> BuscarAsync(
        string buscar,
        string? tipo,
        IReadOnlyList<string> clasificacionesVisibles,
        int limite,
        CancellationToken cancellationToken = default)
    {
        var patron = Patron(buscar);

        var query = dbContext.Diccionario
            .AsNoTracking()
            .Where(d => clasificacionesVisibles.Contains(d.Clasificacion))
            .Where(d =>
                EF.Functions.ILike(d.Nombre, patron, Escape) ||
                EF.Functions.ILike(d.Codigo, patron, Escape) ||
                EF.Functions.ILike(d.Sinonimos ?? string.Empty, patron, Escape));

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var tipoNormalizado = tipo.Trim().ToLowerInvariant();
            query = query.Where(d => d.Tipo == tipoNormalizado);
        }

        return await query
            .OrderBy(d => d.Nombre)
            .Take(limite)
            .ToListAsync(cancellationToken);
    }

    public async Task<Diccionario?> ObtenerPorCodigoAsync(
        string codigo,
        CancellationToken cancellationToken = default) =>
        await dbContext.Diccionario
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Codigo == codigo.Trim(), cancellationToken);

    // Escapa los comodines para que el texto del usuario se busque literalmente;
    // Escape ('\\') se pasa a ILIKE para que PostgreSQL los respete (sin él,
    // Npgsql emite ESCAPE '' y % y _ volverían a ser comodines).
    private const string Escape = "\\";

    private static string Patron(string texto) =>
        $"%{texto
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)}%";
}
