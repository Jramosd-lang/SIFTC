namespace SIFTC.Infrastructure.Notifications;

/// <summary>Configuración SMTP (variables Smtp__Host, Smtp__Port, ...).</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public string? User { get; set; }

    public string? Password { get; set; }

    public string? From { get; set; }

    /// <summary>STARTTLS en el puerto 587; en 465 suele usarse SSL directo.</summary>
    public bool UseStartTls { get; set; } = true;
}

/// <summary>Cuenta de servicio de Firebase (variable Firebase__CredentialsJson).</summary>
public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    /// <summary>Contenido completo del JSON de la cuenta de servicio, en una línea.</summary>
    public string? CredentialsJson { get; set; }
}
