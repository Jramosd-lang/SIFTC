using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIFTC.Application.Common.Interfaces.Notificaciones;

namespace SIFTC.Infrastructure.Notifications;

/// <summary>
/// Push por Firebase Cloud Messaging v1 (CA1). Si no hay cuenta de servicio
/// configurada la API arranca igual y el envío queda registrado como fallo (CA3).
/// </summary>
public sealed class FirebasePushNotificador : IPushNotificador
{
    private const string NombreApp = "siftc-notificaciones";

    private readonly FirebaseMessaging? _mensajeria;
    private readonly ILogger<FirebasePushNotificador> _logger;

    public FirebasePushNotificador(
        IOptions<FirebaseOptions> options,
        ILogger<FirebasePushNotificador> logger)
    {
        _logger = logger;
        var credenciales = options.Value.CredentialsJson;

        if (string.IsNullOrWhiteSpace(credenciales))
        {
            logger.LogWarning(
                "Firebase no configurado: defina Firebase__CredentialsJson para habilitar el push.");
            return;
        }

        try
        {
            var app = FirebaseApp.GetInstance(NombreApp)
                ?? FirebaseApp.Create(
                    new AppOptions
                    {
                        Credential = CredentialFactory
                            .FromJson<ServiceAccountCredential>(credenciales)
                            .ToGoogleCredential()
                    },
                    NombreApp);

            _mensajeria = FirebaseMessaging.GetMessaging(app);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo inicializar Firebase Admin SDK.");
        }
    }

    public async Task EnviarAsync(
        string tokenFcm,
        string asunto,
        string cuerpo,
        CancellationToken cancellationToken = default)
    {
        if (_mensajeria is null)
        {
            throw new NotificacionNoConfiguradaException(
                "Firebase no configurado: defina Firebase__CredentialsJson.");
        }

        var mensaje = new Message
        {
#pragma warning disable CS0618 // FCM sigue usando el registration token; Fid es el Installation ID.
            Token = tokenFcm,
#pragma warning restore CS0618
            Notification = new Notification { Title = asunto, Body = cuerpo },
            // CA1: prioridad alta para que el dispositivo lo reciba de inmediato.
            Android = new AndroidConfig { Priority = Priority.High }
        };

        try
        {
            await _mensajeria.SendAsync(mensaje, cancellationToken);
            _logger.LogInformation("Push enviado (asunto {Asunto}).", asunto);
        }
        catch (FirebaseMessagingException exception)
            when (exception.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            // Token inválido: se desactiva y no se reintenta (CA3).
            throw new TokenPushInvalidoException("El token FCM ya no está registrado.");
        }
    }
}
