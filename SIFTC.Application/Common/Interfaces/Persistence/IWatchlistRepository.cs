using SIFTC.Application.UseCases.Entidades;
using SIFTC.Domain.Entities;

namespace SIFTC.Application.Common.Interfaces.Persistence;

public interface IWatchlistRepository
{
    Task<IReadOnlyList<EntidadDto>> GetByUsuarioAsync(
        Guid idUsuario,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid idUsuario,
        Guid idEntidad,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        EntidadSeguida entidadSeguida,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid idUsuario,
        Guid idEntidad,
        CancellationToken cancellationToken = default);
}
