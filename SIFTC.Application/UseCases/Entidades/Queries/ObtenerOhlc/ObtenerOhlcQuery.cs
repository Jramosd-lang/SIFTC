using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Entidades.Queries.ObtenerOhlc;

public sealed record ObtenerOhlcQuery(
    Guid IdEntidad,
    string? Periodo
) : IRequest<Result<OhlcResponseDto>>;
