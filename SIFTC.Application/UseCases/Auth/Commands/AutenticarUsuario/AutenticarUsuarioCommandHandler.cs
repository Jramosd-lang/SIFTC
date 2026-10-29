using MediatR;
using SIFTC.Application.Common;
using SIFTC.Application.Common.Interfaces;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Auth.Commands.AutenticarUsuario;

public class AutenticarUsuarioCommandHandler(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<AutenticarUsuarioCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(AutenticarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var identificador = request.Email ?? request.Usuario;
        var credencialesInvalidas = new Error("auth.unauthorized", "El usuario o la contraseña son incorrectos.", ErrorType.Unauthorized);

        if (string.IsNullOrWhiteSpace(identificador))
        {
            return Result.Failure<AuthResponseDto>(credencialesInvalidas);
        }

        var usuario = await usuarioRepository.GetByEmailAsync(identificador.Trim());

        if (usuario is null)
        {
            return Result.Failure<AuthResponseDto>(credencialesInvalidas);
        }

        if (!passwordHasher.Verify(request.Password, usuario.PasswordHash))
        {
            return Result.Failure<AuthResponseDto>(credencialesInvalidas);
        }

        var token = jwtTokenGenerator.GenerateToken(usuario);

        var dto = new AuthResponseDto(
            token,
            jwtTokenGenerator.ExpirySeconds,
            new AuthUsuarioDto(
                usuario.IdUsuario,
                $"{usuario.PrimerNombre} {usuario.PrimerApellido}".Trim(),
                usuario.Rol.Nombre));

        return Result.Success(dto);
    }
}
