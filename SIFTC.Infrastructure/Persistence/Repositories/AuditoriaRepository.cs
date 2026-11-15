using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Entities;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public sealed class AuditoriaRepository(ApplicationDbContext dbContext) : IAuditoriaRepository
{
    public async Task RegistrarAsync(Auditoria auditoria, CancellationToken cancellationToken = default)
    {
        await dbContext.Auditorias.AddAsync(auditoria, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
