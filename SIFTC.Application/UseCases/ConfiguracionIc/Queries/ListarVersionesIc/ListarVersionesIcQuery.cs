using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.ConfiguracionIc.Queries.ListarVersionesIc;

public sealed record ListarVersionesIcQuery
    : IRequest<Result<IReadOnlyList<ConfiguracionIcDto>>>;
