using SIFTC.Application.UseCases.ConfiguracionIc;
using SIFTC.Domain.Common;

namespace SIFTC.Application.Common.Interfaces.Persistence;

public interface IConfiguracionIcRepository
{
    Task<ConfiguracionIcDto?> ObtenerActivaAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConfiguracionIcDto>> ListarVersionesAsync(
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CrearVersionAsync(
        Guid idUsuario,
        string motivo,
        string frecuencia,
        IReadOnlyList<PesoIcDto> pesos,
        IReadOnlyList<UmbralIcDto> umbrales,
        CancellationToken cancellationToken = default);
}
