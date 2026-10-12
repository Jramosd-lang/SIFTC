namespace SIFTC.Application.Common;

public record AuthResponseDto(
    Guid IdUsuario,
    string Email,
    string NombreCompleto,
    string Rol,
    string Token
);