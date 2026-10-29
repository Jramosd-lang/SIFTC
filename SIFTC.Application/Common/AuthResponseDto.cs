namespace SIFTC.Application.Common;

public sealed record AuthResponseDto(
    string Token,
    int ExpiraEn,
    AuthUsuarioDto Usuario
);

public sealed record AuthUsuarioDto(
    Guid Id,
    string Nombre,
    string Rol
);
