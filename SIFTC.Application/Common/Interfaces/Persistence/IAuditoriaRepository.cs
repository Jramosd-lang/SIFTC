using SIFTC.Application.UseCases.Auditoria;
using SIFTC.Domain.Entities;

namespace SIFTC.Application.Common.Interfaces.Persistence;

/// <summary>Acceso a la bitácora de auditoría (seguridad.auditorias).</summary>
public interface IAuditoriaRepository
{
    /// <summary>Registra un evento en la bitácora. Lanza excepción si la persistencia falla.</summary>
    Task RegistrarAsync(Auditoria auditoria, CancellationToken cancellationToken = default);

    /// <summary>Devuelve los últimos eventos de la bitácora, del más reciente al más antiguo.</summary>
    Task<IReadOnlyList<AuditoriaDto>> ListarAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
