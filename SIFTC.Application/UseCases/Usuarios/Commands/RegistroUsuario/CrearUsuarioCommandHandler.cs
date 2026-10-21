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
        // 1. Validar que el correo no exista ya en la base de datos
        if (await usuarioRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure<Guid>(new Error("usuario.duplicado", "El correo ya está registrado.", ErrorType.Conflict));
        }

        // 3. Encriptar la contraseña
        var hash = passwordHasher.Hash(request.Password);

        // 4. Construir la entidad (Esto ahora devuelve un Result<Usuario>)
        var nuevoUsuarioResult = Usuario.Create(
            email: request.Email,
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
