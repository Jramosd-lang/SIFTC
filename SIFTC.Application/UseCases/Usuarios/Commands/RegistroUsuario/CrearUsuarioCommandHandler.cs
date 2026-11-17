using MediatR;
using SIFTC.Application.Common.Interfaces;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Usuarios.Commands;
using SIFTC.Domain.Common;
using SIFTC.Domain.Entities;

namespace SIFTC.Application.UseCases.Usuarios.Commands; // <-- Namespace corregido

public class CrearUsuarioCommandHandler(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher)
    : IRequestHandler<CrearUsuarioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CrearUsuarioCommand request, CancellationToken cancellationToken)
    {
        // Mismo criterio que el login y la edición: el correo se guarda en
        // minúsculas, así el índice único no deja pasar variantes de mayúsculas.
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        // 1. Validar que el correo no exista ya en la base de datos
        if (await usuarioRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            return Result.Failure<Guid>(new Error("usuario.duplicado", "El correo ya está registrado.", ErrorType.Conflict));
        }

        // 2. El rol debe existir: sin esto la FK revienta con un 500.
        if (await usuarioRepository.ObtenerNombreRolAsync(request.IdRol, cancellationToken) is null)
        {
            return Result.Failure<Guid>(new Error("usuario.rol.invalido", "El rol indicado no existe.", ErrorType.Validation));
        }

        // 3. Encriptar la contraseña
        var hash = passwordHasher.Hash(request.Password);

        // 4. Construir la entidad (Esto ahora devuelve un Result<Usuario>)
        var nuevoUsuarioResult = Usuario.Create(
            email: email,
            passwordHash: hash,
            primerNombre: request.PrimerNombre,
            primerApellido: request.PrimerApellido,
            idRol: request.IdRol
        );

        // Interceptamos si alguna validación del Dominio falló (ej: correo en blanco)
        if (nuevoUsuarioResult.IsFailure)
        {
            return Result.Failure<Guid>(nuevoUsuarioResult.Error);
        }

        // 5. Extraer el usuario real usando .Value y guardarlo en PostgreSQL
        var usuarioReal = nuevoUsuarioResult.Value;
        await usuarioRepository.AddAsync(usuarioReal, cancellationToken);

        return Result.Success(usuarioReal.IdUsuario);
    }
}
