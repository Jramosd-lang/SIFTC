namespace SIFTC.Application.Common.Interfaces.Notificaciones;

/// <summary>Envío de correo (SMTP).</summary>
public interface ICorreoNotificador
{
    Task EnviarAsync(
        string destino,
        string asunto,
        string cuerpo,
        CancellationToken cancellationToken = default);
}

/// <summary>Envío push por Firebase Cloud Messaging (FCM v1).</summary>
public interface IPushNotificador
{
    Task EnviarAsync(
        string tokenFcm,
        string asunto,
        string cuerpo,
        CancellationToken cancellationToken = default);
}

/// <summary>El token FCM ya no está registrado: se desactiva y no se reintenta.</summary>
public sealed class TokenPushInvalidoException(string mensaje) : Exception(mensaje);

/// <summary>Falta configuración (SMTP o cuenta de servicio de Firebase).</summary>
public sealed class NotificacionNoConfiguradaException(string mensaje) : Exception(mensaje);
