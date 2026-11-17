using MediatR;
using SIFTC.Application.Common;
using SIFTC.Application.Common.Interfaces;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;
using SIFTC.Domain.Entities;
using AuditoriaEntidad = SIFTC.Domain.Entities.Auditoria;

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
            await RegistrarLoginFallidoAsync("Usuario vacío", null, cancellationToken);
            return Result.Failure<AuthResponseDto>(credencialesInvalidas);
        }

        var usuario = await usuarioRepository.GetByEmailAsync(request.Usuario.Trim().ToLowerInvariant());

        if (usuario is null)
        {
            await RegistrarLoginFallidoAsync("Usuario no registrado", null, cancellationToken);
            return Result.Failure<AuthResponseDto>(credencialesInvalidas);
        }

        // Baja lógica: el usuario inactivo no puede iniciar sesión aunque su
        // contraseña siga siendo válida. Mismo mensaje genérico para no revelar
        // el estado de la cuenta.
        if (!string.Equals(usuario.Estado, "activo", StringComparison.OrdinalIgnoreCase))
        {
            await RegistrarLoginFallidoAsync("Usuario inactivo", usuario.IdUsuario, cancellationToken);
            return Result.Failure<AuthResponseDto>(credencialesInvalidas);
        }

        if (!passwordHasher.Verify(request.Password, usuario.PasswordHash))
        {
            await RegistrarLoginFallidoAsync("Contraseña incorrecta", usuario.IdUsuario, cancellationToken);
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

    private async Task RegistrarLoginFallidoAsync(
        string motivo,
        Guid? idUsuario,
        CancellationToken cancellationToken)
    {
        var auditoria = AuditoriaEntidad.Create("login.fallido", motivo, idUsuario);
        if (auditoria.IsFailure)
        {
            return;
        }

        await auditoriaRepository.RegistrarAsync(auditoria.Value, cancellationToken);
    }
}
