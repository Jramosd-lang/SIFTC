using SIFTC.Application.UseCases.Auditoria;

namespace SIFTC.Application.Common.Interfaces.Persistence;

public interface IAuditoriaRepository
{
    Task RegistrarAsync(
        string accion,
        string? detalle,
        Guid? idUsuario,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditoriaDto>> ListarAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
