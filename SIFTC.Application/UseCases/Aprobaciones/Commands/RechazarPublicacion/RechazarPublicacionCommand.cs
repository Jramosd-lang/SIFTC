using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Aprobaciones.Commands.RechazarPublicacion;

public sealed record RechazarPublicacionCommand(
    Guid IdLote,
    Guid IdUsuario,
    string Comentario
) : IRequest<Result>;
