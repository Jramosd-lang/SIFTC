using MediatR;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Application.UseCases.Usuarios;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Usuarios.Queries.ListarUsuarios;

public sealed class ListarUsuariosQueryHandler(IUsuarioRepository usuarioRepository)
    : IRequestHandler<ListarUsuariosQuery, Result<IReadOnlyList<UsuarioDto>>>
{
    public async Task<Result<IReadOnlyList<UsuarioDto>>> Handle(
        ListarUsuariosQuery request,
        CancellationToken cancellationToken)
    {
        var usuarios = await usuarioRepository.ListarActivosAsync(cancellationToken);

        IReadOnlyList<UsuarioDto> dtos = usuarios
            .Select(u => UsuarioDto.Desde(u, u.Rol?.Nombre ?? string.Empty))
            .ToList();

        return Result.Success(dtos);
    }
}
