using Microsoft.EntityFrameworkCore;
using SIFTC.Application.Common.Interfaces;

namespace SIFTC.Infrastructure.Persistence.Repositories;

public abstract class Repository<T>(ApplicationDbContext dbContext) : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext DbContext = dbContext;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<T>().AddAsync(entity, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    public void Update(T entity)
    {
        DbContext.Set<T>().Update(entity);
        DbContext.SaveChanges();
    }

    public void Delete(T entity)
    {
        DbContext.Set<T>().Remove(entity);
        DbContext.SaveChanges();
    }
}