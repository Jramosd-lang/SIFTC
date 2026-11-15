namespace SIFTC.Api.Realtime;

public sealed class TiempoRealOptions
{
    public const string SectionName = "TiempoReal";

    // Habilita POST /api/v1/tiempo-real/emisiones-prueba para la prueba de carga.
    // Por defecto está apagado; en Render se activa solo mientras dura la prueba.
    public bool EmisionPruebaHabilitada { get; init; }
}
