using MediatR;
using SIFTC.Domain.Common;
using SIFTC.Domain.Enums;

namespace SIFTC.Application.UseCases.Usuarios.Commands; // <-- Namespace corregido

public record CrearUsuarioCommand(
    string Email,
    string Password,
    string PrimerNombre,
    string PrimerApellido,
    Rol Rol
) : IRequest<Result<Guid>>;