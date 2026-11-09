using SIFTC.Application.UseCases.Aprobaciones;
using SIFTC.Domain.Common;

namespace SIFTC.Application.Common.Interfaces.Persistence;

public interface IPublicacionRepository
{
    Task<IReadOnlyList<PublicacionPendienteDto>> ListarPendientesAsync(
        CancellationToken cancellationToken = default);

    Task<Result> AprobarAsync(
        Guid idLote,
        Guid idUsuario,
        CancellationToken cancellationToken = default);

    Task<Result> RechazarAsync(
        Guid idLote,
        Guid idUsuario,
        string comentario,
        CancellationToken cancellationToken = default);
}
