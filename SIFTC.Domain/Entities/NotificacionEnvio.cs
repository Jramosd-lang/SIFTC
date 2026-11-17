using SIFTC.Domain.Common;

namespace SIFTC.Domain.Entities;

/// <summary>
/// Fila de la cola y bitácora operativa de envíos (CA1 y CA3): estado, intentos
/// y último error de cada notificación.
/// </summary>
public sealed class NotificacionEnvio
{
    public const int MaximoIntentosPorDefecto = 3;

    public Guid IdEnvio { get; private set; }

    public Guid IdUsuario { get; private set; }

    public Guid? IdRegla { get; private set; }

    /// <summary>correo o push (una regla 'ambos' genera dos filas).</summary>
    public string Canal { get; private set; } = null!;

    public string Asunto { get; private set; } = null!;

    public string Cuerpo { get; private set; } = null!;

    /// <summary>pendiente, enviado o fallido.</summary>
    public string Estado { get; private set; } = null!;

    public int Intentos { get; private set; }

    public int MaxIntentos { get; private set; }

    public string? UltimoError { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public DateTimeOffset? FechaUltimoIntento { get; private set; }

    public DateTimeOffset? FechaEnvio { get; private set; }

    private NotificacionEnvio() { }

    private NotificacionEnvio(
        Guid idUsuario,
        Guid? idRegla,
        string canal,
        string asunto,
        string cuerpo,
        int maxIntentos)
    {
        IdEnvio = Guid.CreateVersion7();
        IdUsuario = idUsuario;
        IdRegla = idRegla;
        Canal = canal;
        Asunto = asunto;
        Cuerpo = cuerpo;
        Estado = "pendiente";
        MaxIntentos = maxIntentos;
        FechaCreacion = DateTimeOffset.UtcNow;
    }

    public static Result<NotificacionEnvio> Crear(
        Guid idUsuario,
        Guid? idRegla,
        string? canal,
        string? asunto,
        string? cuerpo,
        int maxIntentos = MaximoIntentosPorDefecto)
    {
        if (idUsuario == Guid.Empty)
        {
            return Result.Failure<NotificacionEnvio>(new Error(
                "notificacion.usuario.required",
                "El usuario es obligatorio.",
                ErrorType.Validation));
        }

        canal = (canal ?? string.Empty).Trim().ToLowerInvariant();
        if (canal is not ("correo" or "push"))
        {
            return Result.Failure<NotificacionEnvio>(new Error(
                "notificacion.envio.canal.invalid",
                "El canal del envío admite: correo o push.",
                ErrorType.Validation));
        }

        asunto = (asunto ?? string.Empty).Trim();
        cuerpo = (cuerpo ?? string.Empty).Trim();
        if (asunto.Length == 0 || cuerpo.Length == 0)
        {
            return Result.Failure<NotificacionEnvio>(new Error(
                "notificacion.envio.contenido.required",
                "El asunto y el cuerpo son obligatorios.",
                ErrorType.Validation));
        }

        if (maxIntentos < 1)
        {
            maxIntentos = MaximoIntentosPorDefecto;
        }

        return Result.Success(new NotificacionEnvio(
            idUsuario,
            idRegla,
            canal,
            asunto[..Math.Min(asunto.Length, 200)],
            cuerpo,
            maxIntentos));
    }

    public void MarcarEnviado()
    {
        var ahora = DateTimeOffset.UtcNow;
        Estado = "enviado";
        FechaUltimoIntento = ahora;
        FechaEnvio = ahora;
        UltimoError = null;
    }

    /// <summary>
    /// CA3: registra el fallo. Mientras queden intentos la fila sigue 'pendiente';
    /// al agotar max_intentos pasa a 'fallido'.
    /// </summary>
    public void RegistrarFallo(string? error)
    {
        Intentos++;
        FechaUltimoIntento = DateTimeOffset.UtcNow;
        UltimoError = string.IsNullOrWhiteSpace(error)
            ? "Error desconocido al enviar la notificación."
            : error.Trim()[..Math.Min(error.Trim().Length, 500)];

        if (Intentos >= MaxIntentos)
        {
            Estado = "fallido";
        }
    }
}
