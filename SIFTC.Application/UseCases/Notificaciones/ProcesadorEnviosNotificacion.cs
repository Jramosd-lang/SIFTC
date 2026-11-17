using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIFTC.Application.Common.Interfaces.Notificaciones;
using SIFTC.Application.Common.Interfaces.Persistence;
using SIFTC.Domain.Entities;
using AuditoriaEntidad = SIFTC.Domain.Entities.Auditoria;

namespace SIFTC.Application.UseCases.Notificaciones;

/// <summary>
/// CA1: envía los pendientes (correo SMTP o push FCM) en la misma pasada del worker.
/// CA3: cada fallo suma intentos y guarda ultimo_error; al agotar max_intentos la
/// fila queda 'fallido'. Cada intento deja un evento en seguridad.auditorias.
/// </summary>
public sealed class ProcesadorEnviosNotificacion(
    INotificacionRepository repositorio,
    ICorreoNotificador correo,
    IPushNotificador push,
    IAuditoriaRepository auditoria,
    IOptions<NotificacionesOptions> options,
    TimeProvider tiempo,
    ILogger<ProcesadorEnviosNotificacion> logger)
{
    public async Task<int> ProcesarPendientesAsync(CancellationToken cancellationToken = default)
    {
        var opciones = options.Value;
        var reintentoVencidoAntesDe = tiempo.GetUtcNow()
            .AddSeconds(-Math.Max(0, opciones.ReintentoSegundos));

        var pendientes = await repositorio.TomarPendientesAsync(
            Math.Max(1, opciones.LoteMaximo),
            reintentoVencidoAntesDe,
            cancellationToken);

        foreach (var envio in pendientes)
        {
            await ProcesarAsync(envio, cancellationToken);
        }

        return pendientes.Count;
    }

    private async Task ProcesarAsync(NotificacionEnvio envio, CancellationToken cancellationToken)
    {
        try
        {
            if (envio.Canal == "correo")
            {
                await EnviarCorreoAsync(envio, cancellationToken);
            }
            else
            {
                await EnviarPushAsync(envio, cancellationToken);
            }

            envio.MarcarEnviado();
            await repositorio.ActualizarEnvioAsync(envio, cancellationToken);
            await AuditarAsync(envio, "notificacion.enviada", null, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            envio.RegistrarFallo(exception.Message);
            await repositorio.ActualizarEnvioAsync(envio, cancellationToken);

            var accion = envio.Estado == "fallido"
                ? "notificacion.fallida"
                : "notificacion.reintento";

            await AuditarAsync(envio, accion, exception.Message, cancellationToken);

            logger.LogWarning(
                exception,
                "Fallo al enviar la notificación {IdEnvio} por {Canal} (intento {Intento}/{Maximo}).",
                envio.IdEnvio,
                envio.Canal,
                envio.Intentos,
                envio.MaxIntentos);
        }
    }

    private async Task EnviarCorreoAsync(NotificacionEnvio envio, CancellationToken cancellationToken)
    {
        var destino = await repositorio.ObtenerCorreoAsync(envio.IdUsuario, cancellationToken);
        if (string.IsNullOrWhiteSpace(destino))
        {
            throw new InvalidOperationException("El usuario no tiene correo registrado.");
        }

        await correo.EnviarAsync(destino, envio.Asunto, envio.Cuerpo, cancellationToken);
    }

    private async Task EnviarPushAsync(NotificacionEnvio envio, CancellationToken cancellationToken)
    {
        var tokens = await repositorio.ObtenerTokensActivosAsync(envio.IdUsuario, cancellationToken);
        if (tokens.Count == 0)
        {
            throw new InvalidOperationException("El usuario no tiene tokens push activos.");
        }

        var enviados = 0;

        foreach (var token in tokens)
        {
            try
            {
                await push.EnviarAsync(token.TokenFcm, envio.Asunto, envio.Cuerpo, cancellationToken);
                enviados++;
                await repositorio.MarcarTokenUsoAsync(token.IdToken, cancellationToken);
            }
            catch (TokenPushInvalidoException)
            {
                // FCM UNREGISTERED: el token ya no sirve; no se reintenta con él.
                await repositorio.DesactivarTokenAsync(token.TokenFcm, cancellationToken);
            }
        }

        if (enviados == 0)
        {
            throw new InvalidOperationException("Todos los tokens push del usuario son inválidos.");
        }
    }

    private async Task AuditarAsync(
        NotificacionEnvio envio,
        string accion,
        string? error,
        CancellationToken cancellationToken)
    {
        var detalle = $"Envío {envio.IdEnvio} ({envio.Canal}) usuario {envio.IdUsuario}";
        if (!string.IsNullOrWhiteSpace(error))
        {
            detalle += $": {error.Trim()}";
        }

        var auditoriaCreada = AuditoriaEntidad.Create(
            accion,
            detalle[..Math.Min(detalle.Length, 250)],
            envio.IdUsuario);

        if (auditoriaCreada.IsFailure)
        {
            return;
        }

        try
        {
            await auditoria.RegistrarAsync(auditoriaCreada.Value, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // El envío ya ocurrió: un fallo de bitácora no debe reintentarlo ni tumbar el worker.
            logger.LogError(
                exception,
                "No se pudo registrar en auditoría el envío {IdEnvio}.",
                envio.IdEnvio);
        }
    }
}
