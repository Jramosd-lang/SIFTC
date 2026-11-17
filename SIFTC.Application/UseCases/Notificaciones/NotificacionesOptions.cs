namespace SIFTC.Application.UseCases.Notificaciones;

/// <summary>Configuración del worker de envío (sección Notificaciones).</summary>
public sealed class NotificacionesOptions
{
    public const string SectionName = "Notificaciones";

    /// <summary>CA1: cada cuántos segundos revisa la cola. Debe ser ≤ 10.</summary>
    public int IntervaloSegundos { get; set; } = 10;

    /// <summary>Envíos que toma el worker en cada pasada.</summary>
    public int LoteMaximo { get; set; } = 50;

    /// <summary>Espera antes de reintentar un envío fallido (CA3).</summary>
    public int ReintentoSegundos { get; set; } = 30;
}
