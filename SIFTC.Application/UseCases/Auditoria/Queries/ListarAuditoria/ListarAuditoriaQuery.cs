using MediatR;
using SIFTC.Application.UseCases.Auditoria;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Auditoria.Queries.ListarAuditoria;

public sealed record ListarAuditoriaQuery(int? Limit)
    : IRequest<Result<IReadOnlyList<AuditoriaDto>>>;
