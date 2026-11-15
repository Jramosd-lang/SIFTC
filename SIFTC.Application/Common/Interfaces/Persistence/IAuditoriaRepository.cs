using SIFTC.Domain.Entities;

namespace SIFTC.Application.Common.Interfaces.Persistence;

/// <summary>Acceso a la bitácora de auditoría (seguridad.auditorias).</summary>
public interface IAuditoriaRepository
{
    /// <summary>Registra un evento en la bitácora. Lanza excepción si la persistencia falla.</summary>
    Task RegistrarAsync(Auditoria auditoria, CancellationToken cancellationToken = default);
}
