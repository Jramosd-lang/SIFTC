using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Notificaciones.Commands.EliminarRegla;

/// <summary>CA2: desactiva una regla propia.</summary>
public sealed record EliminarReglaCommand(Guid IdUsuario, Guid IdRegla) : IRequest<Result>;
