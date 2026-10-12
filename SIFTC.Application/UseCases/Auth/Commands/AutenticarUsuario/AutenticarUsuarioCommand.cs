using MediatR;
using SIFTC.Application.Common;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Auth.Commands.AutenticarUsuario;

public record AutenticarUsuarioCommand(
    string Email,
    string Password
) : IRequest<Result<AuthResponseDto>>;