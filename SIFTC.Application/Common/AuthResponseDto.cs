namespace SIFTC.Application.Common;

/// <summary>Respuesta del login.</summary>
/// <param name="Token">JWT que debe enviarse como Bearer en los demás endpoints.</param>
/// <param name="ExpiraEn">Vigencia del token en segundos.</param>
/// <param name="Usuario">Datos del usuario autenticado.</param>
public sealed record AuthResponseDto(
    string Token,
    int ExpiraEn,
    AuthUsuarioDto Usuario
);

/// <summary>Datos mínimos del usuario autenticado.</summary>
/// <param name="Id">Identificador UUID del usuario.</param>
/// <param name="Nombre">Nombre completo.</param>
/// <param name="Rol">Rol del sistema; determina los permisos.</param>
public sealed record AuthUsuarioDto(
    Guid Id,
    string Nombre,
    string Rol
);
