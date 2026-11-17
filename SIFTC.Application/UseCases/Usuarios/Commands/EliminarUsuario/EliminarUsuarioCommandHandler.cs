using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Common;
using AuditoriaEntidad = SIFTC.Domain.Entities.Auditoria;

namespace SIFTC.Application.UseCases.Usuarios.Commands.EliminarUsuario;

public sealed class EliminarUsuarioCommandHandler(
    IUsuarioRepository usuarioRepository,
    IAuditoriaRepository auditoriaRepository)
    : IRequestHandler<EliminarUsuarioCommand, Result>
{
    private const string RolAdministrador = "Administrador";

    public async Task<Result> Handle(
        EliminarUsuarioCommand request,
        CancellationToken cancellationToken)
    {
        // Un administrador no puede sacarse a sí mismo: evita quedar bloqueado.
        if (request.IdSolicitante == request.IdUsuario)
        {
            return Result.Failure(new Error(
                "usuario.autoeliminar",
                "No puedes desactivar tu propio usuario.",
                ErrorType.Validation));
        }

        var usuario = await usuarioRepository.GetByIdAsync(request.IdUsuario, cancellationToken);
        if (usuario is null)
        {
            return Result.Failure(new Error(
                "usuario.no.encontrado",
                "El usuario no existe.",
                ErrorType.NotFound));
        }

        // Ya estaba inactivo: la baja es idempotente.
        if (!string.Equals(usuario.Estado, "activo", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Success();
        }

        // No dejar el sistema sin ningún administrador activo.
        var nombreRol = await usuarioRepository.ObtenerNombreRolAsync(usuario.IdRol, cancellationToken);
        if (string.Equals(nombreRol, RolAdministrador, StringComparison.Ordinal))
        {
            var otrosAdministradores = await usuarioRepository.ContarActivosPorRolAsync(
                usuario.IdRol, usuario.IdUsuario, cancellationToken);

            if (otrosAdministradores == 0)
            {
                return Result.Failure(new Error(
                    "usuario.ultimo.administrador",
                    "No se puede desactivar al último administrador activo.",
                    ErrorType.Conflict));
            }
        }

        usuario.Deactivate();
        await usuarioRepository.UpdateAsync(usuario, cancellationToken);

        var auditoria = AuditoriaEntidad.Create(
            "usuario.desactivado",
            $"Usuario {usuario.IdUsuario} ({usuario.Email}) pasó a estado inactivo.",
            request.IdSolicitante);

        if (auditoria.IsSuccess)
        {
            await auditoriaRepository.RegistrarAsync(auditoria.Value, cancellationToken);
        }

        return Result.Success();
    }
}
