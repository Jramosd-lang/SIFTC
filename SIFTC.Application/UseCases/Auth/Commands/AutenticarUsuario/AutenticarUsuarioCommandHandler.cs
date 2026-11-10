using MediatR;
using SIFTC.Application.Common;
using SIFTC.Application.Common.Interfaces;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Auth.Commands.AutenticarUsuario;

public class AutenticarUsuarioCommandHandler(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IAuditoriaRepository auditoriaRepository)
    : IRequestHandler<AutenticarUsuarioCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(AutenticarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var credencialesInvalidas = new Error("auth.unauthorized", "El usuario o la contraseña son incorrectos.", ErrorType.Unauthorized);

        if (string.IsNullOrWhiteSpace(request.Usuario))
        {
            await auditoriaRepository.RegistrarAsync("login.fallido", "Usuario vacío", null, cancellationToken);
            return Result.Failure<AuthResponseDto>(credencialesInvalidas);
        }

        var usuario = await usuarioRepository.GetByEmailAsync(request.Usuario.Trim());

        if (usuario is null)
        {
            await auditoriaRepository.RegistrarAsync("login.fallido", "Usuario no registrado", null, cancellationToken);
            return Result.Failure<AuthResponseDto>(credencialesInvalidas);
        }

        if (!passwordHasher.Verify(request.Password, usuario.PasswordHash))
        {
            await auditoriaRepository.RegistrarAsync("login.fallido", "Contraseña incorrecta", usuario.IdUsuario, cancellationToken);
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
