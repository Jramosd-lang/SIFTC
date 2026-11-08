using MediatR;
using SIFTC.Application.Common;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Auth.Commands.AutenticarUsuario;

/// <summary>Credenciales de acceso.</summary>
/// <param name="Usuario">Correo registrado. Es el identificador de login; no existe nombre de usuario separado.</param>
/// <param name="Password">Contraseña en texto plano; se valida contra el hash bcrypt.</param>
public sealed record AutenticarUsuarioCommand(
    string Usuario,
    string Password
) : IRequest<Result<AuthResponseDto>>;
