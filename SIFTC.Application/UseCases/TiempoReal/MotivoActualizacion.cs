namespace SIFTC.Application.UseCases.TiempoReal;

/// <summary>Por qué se envía una actualización de cotizaciones por tiempo real.</summary>
public static class MotivoActualizacion
{
    /// <summary>Estado completo que recibe un cliente al conectarse o reconectarse.</summary>
    public const string EstadoInicial = "estado-inicial";

    /// <summary>Un Analista financiero aprobó un lote y se publicaron nuevos valores del IC.</summary>
    public const string LotePublicado = "lote-publicado";

    /// <summary>Emisión manual para la prueba de carga; no cambia los datos.</summary>
    public const string Prueba = "prueba";
}
