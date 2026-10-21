using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Usuarios.Commands; // <-- Namespace corregido

public record CrearUsuarioCommand(
    string Email,
    string Password,
    string PrimerNombre,
    string PrimerApellido,
    Guid IdRol
) : IRequest<Result<Guid>>;
