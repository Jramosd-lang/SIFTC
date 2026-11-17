using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Usuarios.Commands.EliminarUsuario;

/// <summary>
/// Baja lógica de un usuario: NO borra la fila, la deja en estado 'inactivo'
/// (así conserva reglas, envíos y auditoría). Es idempotente.
/// </summary>
/// <param name="IdUsuario">UUID del usuario a desactivar (viene de la ruta).</param>
/// <param name="IdSolicitante">UUID del administrador que ejecuta la baja.</param>
public sealed record EliminarUsuarioCommand(
    Guid IdUsuario,
    Guid IdSolicitante
) : IRequest<Result>;
