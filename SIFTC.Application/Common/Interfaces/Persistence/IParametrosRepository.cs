using SIFTC.Application.UseCases.Parametros;

namespace SIFTC.Application.Common.Interfaces.Persistence;

public interface IParametrosRepository
{
    Task<ParametrosDto> ObtenerAsync(CancellationToken cancellationToken = default);
}
