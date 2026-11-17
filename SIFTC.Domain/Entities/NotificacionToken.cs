using SIFTC.Domain.Common;

namespace SIFTC.Domain.Entities;

/// <summary>
/// Token FCM de un dispositivo del usuario (web/android/ios). No se registra en
/// bitácoras ni logs: es un dato sensible.
/// </summary>
public sealed class NotificacionToken
{
    private static readonly string[] PlataformasValidas = ["web", "android", "ios"];

    public Guid IdToken { get; private set; }

    public Guid IdUsuario { get; private set; }

    /// <summary>web, android o ios.</summary>
    public string Plataforma { get; private set; } = null!;

    public string TokenFcm { get; private set; } = null!;

    public bool Activo { get; private set; }

    public DateTimeOffset FechaRegistro { get; private set; }

    public DateTimeOffset? UltimoUso { get; private set; }

    private NotificacionToken() { }

    private NotificacionToken(Guid idUsuario, string plataforma, string tokenFcm)
    {
        IdToken = Guid.CreateVersion7();
        IdUsuario = idUsuario;
        Plataforma = plataforma;
        TokenFcm = tokenFcm;
        Activo = true;
        FechaRegistro = DateTimeOffset.UtcNow;
    }

    public static Result<NotificacionToken> Crear(Guid idUsuario, string? plataforma, string? tokenFcm)
    {
        if (idUsuario == Guid.Empty)
        {
            return Result.Failure<NotificacionToken>(new Error(
                "notificacion.usuario.required",
                "El usuario es obligatorio.",
                ErrorType.Validation));
        }

        plataforma = (plataforma ?? string.Empty).Trim().ToLowerInvariant();
        if (!PlataformasValidas.Contains(plataforma, StringComparer.Ordinal))
        {
            return Result.Failure<NotificacionToken>(new Error(
                "notificacion.token.plataforma.invalid",
                "La plataforma admite: web, android o ios.",
                ErrorType.Validation));
        }

        tokenFcm = (tokenFcm ?? string.Empty).Trim();
        if (tokenFcm.Length == 0)
        {
            return Result.Failure<NotificacionToken>(new Error(
                "notificacion.token.required",
                "El token FCM es obligatorio.",
                ErrorType.Validation));
        }

        return Result.Success(new NotificacionToken(idUsuario, plataforma, tokenFcm));
    }

    /// <summary>Un token puede reasignarse si el mismo dispositivo cambia de sesión.</summary>
    public void Reasignar(Guid idUsuario, string plataforma)
    {
        IdUsuario = idUsuario;
        Plataforma = plataforma;
        Activo = true;
    }

    public void MarcarUso() => UltimoUso = DateTimeOffset.UtcNow;

    public void Desactivar() => Activo = false;
}
