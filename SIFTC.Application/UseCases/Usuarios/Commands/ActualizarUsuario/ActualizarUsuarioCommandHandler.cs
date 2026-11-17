using MediatR;
using SIFTC.Application.Common.Interfaces;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Usuarios;
using SIFTC.Domain.Common;
using AuditoriaEntidad = SIFTC.Domain.Entities.Auditoria;

namespace SIFTC.Application.UseCases.Usuarios.Commands.ActualizarUsuario;

public sealed class ActualizarUsuarioCommandHandler(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    IAuditoriaRepository auditoriaRepository)
    : IRequestHandler<ActualizarUsuarioCommand, Result<UsuarioDto>>
{
    public async Task<Result<UsuarioDto>> Handle(
        ActualizarUsuarioCommand request,
        CancellationToken cancellationToken)
    {
        var usuario = await usuarioRepository.GetByIdAsync(request.IdUsuario, cancellationToken);
        if (usuario is null)
        {
            return Result.Failure<UsuarioDto>(new Error(
                "usuario.no.encontrado",
                "El usuario no existe.",
                ErrorType.NotFound));
        }

        // Las validaciones de correo, nombres y rol viven en el dominio.
        var actualizacion = usuario.ActualizarDatos(
            request.Email,
            request.PrimerNombre,
            request.SegundoNombre,
            request.PrimerApellido,
            request.SegundoApellido,
            request.IdRol);

        if (actualizacion.IsFailure)
        {
            return Result.Failure<UsuarioDto>(actualizacion.Error);
        }

        // El correo es el identificador de login: no puede quedar repetido.
        if (await usuarioRepository.ExistsByEmailExceptoAsync(
                usuario.Email, usuario.IdUsuario, cancellationToken))
        {
            return Result.Failure<UsuarioDto>(new Error(
                "usuario.duplicado",
                "El correo ya está registrado por otro usuario.",
                ErrorType.Conflict));
        }

        var nombreRol = await usuarioRepository.ObtenerNombreRolAsync(request.IdRol, cancellationToken);
        if (nombreRol is null)
        {
            return Result.Failure<UsuarioDto>(new Error(
                "usuario.rol.invalido",
                "El rol indicado no existe.",
                ErrorType.Validation));
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            usuario.SetPasswordHash(passwordHasher.Hash(request.Password));
        }

        await usuarioRepository.UpdateAsync(usuario, cancellationToken);

        await RegistrarAuditoriaAsync(
            "usuario.actualizado",
            $"Usuario {usuario.IdUsuario} actualizado (correo {usuario.Email}, rol {nombreRol}).",
            cancellationToken);

        return Result.Success(UsuarioDto.Desde(usuario, nombreRol));
    }

    private async Task RegistrarAuditoriaAsync(
        string accion,
        string detalle,
        CancellationToken cancellationToken)
    {
        var auditoria = AuditoriaEntidad.Create(accion, detalle, null);
        if (auditoria.IsSuccess)
        {
            await auditoriaRepository.RegistrarAsync(auditoria.Value, cancellationToken);
        }
    }
}
