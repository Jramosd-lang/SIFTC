using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.ConfiguracionIc.Queries.ObtenerConfiguracionIc;

public sealed record ObtenerConfiguracionIcQuery
    : IRequest<Result<ConfiguracionIcDto>>;
