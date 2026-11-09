using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Aprobaciones.Commands.AprobarPublicacion;

public sealed record AprobarPublicacionCommand(
    Guid IdLote,
    Guid IdUsuario
) : IRequest<Result>;
