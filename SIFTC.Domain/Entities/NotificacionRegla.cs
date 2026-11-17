using SIFTC.Domain.Common;

namespace SIFTC.Domain.Entities;

/// <summary>
/// Regla de notificación por usuario (HU-AD-10 / HU-MO-05, CA2): canal, entidad
/// y umbral de variación que dispara el aviso.
/// </summary>
public sealed class NotificacionRegla
{
    private static readonly string[] CanalesValidos = ["correo", "push", "ambos"];

    public Guid IdRegla { get; private set; }

    public Guid IdUsuario { get; private set; }

    /// <summary>correo, push o ambos.</summary>
    public string Canal { get; private set; } = null!;

    /// <summary>NULL = todas las entidades.</summary>
    public Guid? IdEntidad { get; private set; }

    /// <summary>Variación mínima en porcentaje; NULL = cualquier cambio.</summary>
    public decimal? Umbral { get; private set; }

    public bool Activa { get; private set; }

    public DateTimeOffset FechaCreacion { get; private set; }

    public DateTimeOffset FechaActualizacion { get; private set; }

    private NotificacionRegla() { }

    private NotificacionRegla(Guid idUsuario, string canal, Guid? idEntidad, decimal? umbral)
    {
        IdRegla = Guid.CreateVersion7();
        IdUsuario = idUsuario;
        Canal = canal;
        IdEntidad = idEntidad;
        Umbral = umbral;
        Activa = true;
        FechaCreacion = DateTimeOffset.UtcNow;
        FechaActualizacion = FechaCreacion;
    }

    public static Result<NotificacionRegla> Crear(
        Guid idUsuario,
        string? canal,
        Guid? idEntidad,
        decimal? umbral)
    {
        if (idUsuario == Guid.Empty)
        {
            return Result.Failure<NotificacionRegla>(new Error(
                "notificacion.usuario.required",
                "El usuario es obligatorio.",
                ErrorType.Validation));
        }

        canal = (canal ?? string.Empty).Trim().ToLowerInvariant();
        if (!CanalesValidos.Contains(canal, StringComparer.Ordinal))
        {
            return Result.Failure<NotificacionRegla>(new Error(
                "notificacion.canal.invalid",
                "El canal admite: correo, push o ambos.",
                ErrorType.Validation));
        }

        if (umbral is < 0)
        {
            return Result.Failure<NotificacionRegla>(new Error(
                "notificacion.umbral.invalid",
                "El umbral debe ser mayor o igual a cero.",
                ErrorType.Validation));
        }

        if (idEntidad == Guid.Empty)
        {
            idEntidad = null;
        }

        return Result.Success(new NotificacionRegla(idUsuario, canal, idEntidad, umbral));
    }

    /// <summary>Indica si la regla cubre el evento (entidad y umbral).</summary>
    public bool AplicaA(Guid idEntidad, decimal variacionPct)
    {
        if (!Activa)
        {
            return false;
        }

        if (IdEntidad is not null && IdEntidad != idEntidad)
        {
            return false;
        }

        return Umbral is null || Math.Abs(variacionPct) >= Umbral.Value;
    }

    /// <summary>Canales concretos del envío: 'ambos' produce dos.</summary>
    public IReadOnlyList<string> Canales() => Canal switch
    {
        "ambos" => ["correo", "push"],
        _ => [Canal]
    };

    public void Desactivar()
    {
        Activa = false;
        FechaActualizacion = DateTimeOffset.UtcNow;
    }

    /// <summary>Reactiva una regla desactivada con el umbral nuevo (el índice único no admite duplicados).</summary>
    public void Reactivar(decimal? umbral)
    {
        Umbral = umbral;
        Activa = true;
        FechaActualizacion = DateTimeOffset.UtcNow;
    }
}
