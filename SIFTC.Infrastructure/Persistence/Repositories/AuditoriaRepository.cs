using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Auditoria;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class AuditoriaRepository(ApplicationDbContext dbContext) : IAuditoriaRepository
{
    public async Task RegistrarAsync(
        string accion,
        string? detalle,
        Guid? idUsuario,
        CancellationToken cancellationToken = default)
    {
        var resultado = Auditoria.Create(accion, detalle, idUsuario);
        if (resultado.IsFailure)
        {
            return;
        }

        await dbContext.Auditorias.AddAsync(resultado.Value, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditoriaDto>> ListarAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Auditorias
            .AsNoTracking()
            .OrderByDescending(a => a.FechaEvento)
            .Take(limit)
            .Select(a => new AuditoriaDto(a.IdBitacora, a.Accion, a.Detalle, a.FechaEvento, a.IdUsuario))
            .ToListAsync(cancellationToken);
    }
}
