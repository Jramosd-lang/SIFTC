using MediatR;
using SIFTC.Application.UseCases.MatrizCruda;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.MatrizCruda.Queries.ListarMatrizCruda;

public sealed record ListarMatrizCrudaQuery(int? Limit)
    : IRequest<Result<IReadOnlyList<MatrizCrudaDto>>>;
