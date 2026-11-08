using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Aprobaciones.Queries.ListarPendientes;

public sealed record ListarPendientesQuery
    : IRequest<Result<IReadOnlyList<PublicacionPendienteDto>>>;
