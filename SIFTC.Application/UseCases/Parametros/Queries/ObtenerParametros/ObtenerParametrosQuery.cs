using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Parametros.Queries.ObtenerParametros;

public sealed record ObtenerParametrosQuery : IRequest<Result<ParametrosDto>>;
