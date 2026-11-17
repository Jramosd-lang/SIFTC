using MediatR;
using SIFTC.Application.UseCases.Usuarios;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Usuarios.Queries.ListarUsuarios;

/// <summary>
/// Listado para el front: solo usuarios con estado 'activo'. Los inactivos
/// (baja lógica) no se devuelven.
/// </summary>
public sealed record ListarUsuariosQuery : IRequest<Result<IReadOnlyList<UsuarioDto>>>;
