using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SIFTC.Application.Common.Interfaces.Notificaciones;

namespace SIFTC.Infrastructure.Notifications;

/// <summary>Envío de correo por SMTP con MailKit (CA1).</summary>
public sealed class SmtpCorreoNotificador(
    IOptions<SmtpOptions> options,
    ILogger<SmtpCorreoNotificador> logger) : ICorreoNotificador
{
    public async Task EnviarAsync(
        string destino,
        string asunto,
        string cuerpo,
        CancellationToken cancellationToken = default)
    {
        var configuracion = options.Value;

        if (string.IsNullOrWhiteSpace(configuracion.Host) ||
            string.IsNullOrWhiteSpace(configuracion.From))
        {
            throw new NotificacionNoConfiguradaException(
                "SMTP no configurado: defina Smtp__Host y Smtp__From.");
        }

        var mensaje = new MimeMessage();
        mensaje.From.Add(MailboxAddress.Parse(configuracion.From));
        mensaje.To.Add(MailboxAddress.Parse(destino));
        mensaje.Subject = asunto;
        mensaje.Body = new TextPart("plain") { Text = cuerpo };

        using var cliente = new SmtpClient();
        var seguridad = configuracion.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await cliente.ConnectAsync(
            configuracion.Host,
            configuracion.Port,
            seguridad,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(configuracion.User))
        {
            await cliente.AuthenticateAsync(
                configuracion.User,
                configuracion.Password ?? string.Empty,
                cancellationToken);
        }

        await cliente.SendAsync(FormatOptions.Default, new SendRequest(mensaje), cancellationToken);
        await cliente.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Correo enviado a {Destino} con asunto {Asunto}.", destino, asunto);
    }
}
